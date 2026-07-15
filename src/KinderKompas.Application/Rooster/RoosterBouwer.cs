using KinderKompas.Application.Planning;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Rooster;

/// <summary>
/// Bouwt de rooster-weergavedata uit reeds geladen domeingegevens. Pure functie zonder
/// database- of UI-afhankelijkheid. De BKR-behoefte (nodig pm'ers) komt ONGEWIJZIGD uit
/// de <see cref="WeekplanningDto"/> — die op zijn beurt uit de
/// <see cref="Domain.Services.BkrCalculator"/> komt — zodat de indicator boven het rooster
/// aantoonbaar gelijk is aan de domein-calculator. De cel-kleuren worden afgeleid uit
/// verlof en ziekte.
/// </summary>
public static class RoosterBouwer
{
    /// <summary>Vertaalt de weekplanning naar de BKR-behoefte per groep per dag (input voor de generator).</summary>
    public static IReadOnlyList<GroepDagBehoefte> BehoeftenUit(WeekplanningDto weekplanning)
    {
        ArgumentNullException.ThrowIfNull(weekplanning);

        var behoeften = new List<GroepDagBehoefte>();
        foreach (StamgroepWeekDto groep in weekplanning.Stamgroepen)
        {
            foreach (DagPlanningDto dag in groep.Dagen)
            {
                behoeften.Add(new GroepDagBehoefte(
                    groep.StamgroepId, dag.Datum, dag.Bkr.VereisteHoeveelheidPmers ?? 0));
            }
        }

        return behoeften;
    }

    public static RoosterWeekDto Bouw(
        WeekplanningDto weekplanning,
        Roosterweek? roosterweek,
        IReadOnlyList<Roosterdienst> diensten,
        IReadOnlyList<Medewerker> medewerkers,
        IReadOnlyList<Verlofaanvraag> verlof,
        IReadOnlyList<Ziekmelding> ziek)
    {
        ArgumentNullException.ThrowIfNull(weekplanning);
        ArgumentNullException.ThrowIfNull(diensten);
        ArgumentNullException.ThrowIfNull(medewerkers);
        ArgumentNullException.ThrowIfNull(verlof);
        ArgumentNullException.ThrowIfNull(ziek);

        Dictionary<Guid, Medewerker> medewerkerPerId = medewerkers.ToDictionary(m => m.Id);

        var groepen = new List<RoosterGroepDto>();
        foreach (StamgroepWeekDto groep in weekplanning.Stamgroepen)
        {
            List<Roosterdienst> dienstenInGroep = diensten.Where(d => d.StamgroepId == groep.StamgroepId).ToList();

            var indicatoren = groep.Dagen.Select(dag =>
            {
                // Alleen medewerkers die meetellen voor de BKR tellen als "ingeplande
                // begeleider" (een stagiair met TeltMeeVoorBkr=false telt niet mee).
                int ingepland = dienstenInGroep.Count(d => d.Datum == dag.Datum
                    && (!medewerkerPerId.TryGetValue(d.MedewerkerId, out Medewerker? mw) || mw.TeltMeeVoorBkr));
                int? nodig = dag.Bkr.VereisteHoeveelheidPmers;
                return new RoosterDagIndicatorDto(
                    dag.Datum, dag.Dag, dag.Bkr.AantalKinderen, nodig, ingepland,
                    dag.Bkr.Overschrijdt, BepaalIndicatorKleur(nodig, ingepland, dag.Bkr.Overschrijdt));
            }).ToList();

            // Rijen: medewerkers met deze groep als thuisgroep, plus iedereen met een
            // dienst in deze groep deze week (bijgeplaatste krachten).
            var medewerkerIds = new HashSet<Guid>(
                medewerkers.Where(m => m.VasteStamgroepId == groep.StamgroepId).Select(m => m.Id));
            foreach (Roosterdienst d in dienstenInGroep)
            {
                medewerkerIds.Add(d.MedewerkerId);
            }

            var rijen = medewerkerIds
                .Select(id => medewerkerPerId.TryGetValue(id, out Medewerker? m) ? m : null)
                .Where(m => m is not null)
                .Select(m => m!)
                .OrderBy(m => m.Achternaam).ThenBy(m => m.Voornaam)
                .Select(m => BouwRij(m, groep, dienstenInGroep, verlof, ziek))
                .ToList();

            groepen.Add(new RoosterGroepDto(groep.StamgroepId, groep.Naam, indicatoren, rijen));
        }

        return new RoosterWeekDto(
            weekplanning.WeekBegin,
            roosterweek is not null,
            roosterweek?.Id,
            roosterweek?.Status,
            roosterweek?.VerstuurdOp,
            groepen);
    }

    private static RoosterMedewerkerRijDto BouwRij(
        Medewerker m, StamgroepWeekDto groep, List<Roosterdienst> dienstenInGroep,
        IReadOnlyList<Verlofaanvraag> verlof, IReadOnlyList<Ziekmelding> ziek)
    {
        var cellen = groep.Dagen.Select(dag =>
        {
            Roosterdienst? dienst = dienstenInGroep
                .FirstOrDefault(d => d.MedewerkerId == m.Id && d.Datum == dag.Datum);

            RoosterCelKleur kleur = BepaalCelKleur(m.Id, dag.Datum, dienst, verlof, ziek);
            return new RoosterCelDto(
                dag.Datum, dag.Dag, kleur,
                dienst?.Id, dienst?.Taakomschrijving, dienst?.UrencorrectieKwartieren ?? 0,
                dienst?.Dienstsoort ?? Dienstsoort.Regulier,
                dienst?.EffectieveBegintijd, dienst?.EffectieveEindtijd, dienst?.GeplandeUren);
        }).ToList();

        return new RoosterMedewerkerRijDto(m.Id, $"{m.Voornaam} {m.Achternaam}", cellen);
    }

    private static RoosterCelKleur BepaalCelKleur(
        Guid medewerkerId, DateOnly datum, Roosterdienst? dienst,
        IReadOnlyList<Verlofaanvraag> verlof, IReadOnlyList<Ziekmelding> ziek)
    {
        // Ziekte en verlof krijgen voorrang op een (eventueel handmatig) ingeplande dienst,
        // zodat een conflict zichtbaar wordt.
        if (ziek.Any(z => z.MedewerkerId == medewerkerId && z.OmvatDatum(datum)))
        {
            return RoosterCelKleur.Ziek;
        }

        Verlofaanvraag? aanvraag = verlof
            .Where(v => v.MedewerkerId == medewerkerId && v.OmvatDatum(datum))
            .OrderBy(v => v.Status) // Openstaand(0) < Goedgekeurd(1) < Afgekeurd(2)
            .FirstOrDefault();
        if (aanvraag is not null)
        {
            switch (aanvraag.Status)
            {
                case Domain.Enums.VerlofStatus.Goedgekeurd: return RoosterCelKleur.VerlofGoedgekeurd;
                case Domain.Enums.VerlofStatus.Openstaand: return RoosterCelKleur.VerlofAangevraagd;
            }
        }

        return dienst is not null ? RoosterCelKleur.Standaard : RoosterCelKleur.Leeg;
    }

    private static BkrIndicatorKleur BepaalIndicatorKleur(int? nodig, int ingepland, bool overschrijdt)
    {
        if (overschrijdt)
        {
            return BkrIndicatorKleur.Rood; // groep boven wettelijk maximum
        }
        if (nodig is not { } n || n == 0)
        {
            return BkrIndicatorKleur.Groen; // geen kinderen / geen behoefte
        }
        if (ingepland < n)
        {
            return BkrIndicatorKleur.Rood; // tekort
        }
        return ingepland == n ? BkrIndicatorKleur.Oranje : BkrIndicatorKleur.Groen;
    }
}
