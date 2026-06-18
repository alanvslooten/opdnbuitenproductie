using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Genereert — puur en deterministisch — een auto-rooster-VOORSTEL uit het
/// basisrooster van de medewerkers, de BKR-behoefte per groep per dag, en het
/// goedgekeurde verlof en de ziekmeldingen. Geen database- of UI-afhankelijkheid.
///
/// Twee harde regels uit het bouwplan (fix t.o.v. v1):
///   1. BKR-behoefte is LEIDEND. Beschikbaarheid leidt niet automatisch tot inplannen;
///      er wordt alleen vanuit de beschikbaarheidslaag bijgeplaatst als de vaste
///      bezetting onvoldoende is om de vereiste pm'ers te halen.
///   2. Goedgekeurd verlof (en ziekte) wordt ALTIJD gerespecteerd: zo iemand wordt
///      nooit ingepland — ook niet als de BKR daardoor niet gehaald wordt. Het
///      voorstel is advies; de planner beslist.
///
/// De vaste bezetting wordt NIET getrimd als die groter is dan strikt nodig: beide
/// vaste leidsters blijven in het voorstel staan, ook als de BKR er maar 1 vraagt.
/// </summary>
public static class RoosterGenerator
{
    public static IReadOnlyList<RoosterVoorstelRegel> GenereerVoorstel(
        IEnumerable<Medewerker> medewerkers,
        IEnumerable<GroepDagBehoefte> behoeften,
        IEnumerable<Verlofaanvraag> goedgekeurdVerlof,
        IEnumerable<Ziekmelding> ziekmeldingen)
    {
        ArgumentNullException.ThrowIfNull(medewerkers);
        ArgumentNullException.ThrowIfNull(behoeften);
        ArgumentNullException.ThrowIfNull(goedgekeurdVerlof);
        ArgumentNullException.ThrowIfNull(ziekmeldingen);

        List<Medewerker> alle = medewerkers.ToList();
        List<Verlofaanvraag> verlof = goedgekeurdVerlof.Where(v => v.IsGoedgekeurd).ToList();
        List<Ziekmelding> ziek = ziekmeldingen.ToList();

        // Stabiele volgorde: per datum, dan per groep — zodat de uitkomst deterministisch is.
        List<GroepDagBehoefte> volgorde = behoeften
            .OrderBy(b => b.Datum).ThenBy(b => b.StamgroepId)
            .ToList();

        var regels = new List<RoosterVoorstelRegel>();
        // Voorkomt dubbele inzet van dezelfde medewerker op dezelfde dag (over groepen heen).
        var ingezetPerDag = new Dictionary<DateOnly, HashSet<Guid>>();

        HashSet<Guid> IngezetOp(DateOnly datum) =>
            ingezetPerDag.TryGetValue(datum, out HashSet<Guid>? set) ? set : ingezetPerDag[datum] = new();

        bool Geblokkeerd(Medewerker m, DateOnly datum) =>
            verlof.Any(v => v.MedewerkerId == m.Id && v.OmvatDatum(datum)) ||
            ziek.Any(z => z.MedewerkerId == m.Id && z.OmvatDatum(datum));

        // --- Pass 1: vaste bezetting plaatsen (eerst over alle groepen/dagen) ---
        // Zo claimt de vaste inzet een medewerker vóór de beschikbaarheidslaag eraan komt.
        foreach (GroepDagBehoefte behoefte in volgorde)
        {
            Weekdag dag = Aanwezigheid.NaarWeekdag(behoefte.Datum);
            if (dag == Weekdag.Geen)
            {
                continue;
            }

            foreach (Medewerker m in alle
                .Where(m => m.VasteStamgroepId == behoefte.StamgroepId && m.WerktVastOp(dag))
                .OrderBy(m => m.Achternaam).ThenBy(m => m.Voornaam))
            {
                if (Geblokkeerd(m, behoefte.Datum))
                {
                    continue; // nooit inplannen op goedgekeurd verlof of ziekte
                }

                regels.Add(new RoosterVoorstelRegel(behoefte.StamgroepId, behoefte.Datum, m.Id, RoosterBron.Vast));
                IngezetOp(behoefte.Datum).Add(m.Id);
            }
        }

        // --- Pass 2: tekorten aanvullen vanuit de beschikbaarheidslaag ---
        foreach (GroepDagBehoefte behoefte in volgorde)
        {
            Weekdag dag = Aanwezigheid.NaarWeekdag(behoefte.Datum);
            if (dag == Weekdag.Geen)
            {
                continue;
            }

            int ingepland = regels.Count(r => r.StamgroepId == behoefte.StamgroepId && r.Datum == behoefte.Datum);
            int tekort = behoefte.NodigPmers - ingepland;
            if (tekort <= 0)
            {
                continue;
            }

            HashSet<Guid> ingezet = IngezetOp(behoefte.Datum);
            IEnumerable<Medewerker> kandidaten = alle
                .Where(m => m.IsBeschikbaarOp(dag) && !ingezet.Contains(m.Id) && !Geblokkeerd(m, behoefte.Datum))
                // Eigen thuisgroep eerst, daarna op naam — deterministisch.
                .OrderByDescending(m => m.VasteStamgroepId == behoefte.StamgroepId)
                .ThenBy(m => m.Achternaam).ThenBy(m => m.Voornaam);

            foreach (Medewerker m in kandidaten.Take(tekort))
            {
                regels.Add(new RoosterVoorstelRegel(behoefte.StamgroepId, behoefte.Datum, m.Id, RoosterBron.Beschikbaar));
                ingezet.Add(m.Id);
            }
        }

        return regels;
    }
}
