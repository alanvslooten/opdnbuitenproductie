using KinderKompas.Application.Planning;
using KinderKompas.Application.Rooster;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Tests.Rooster;

/// <summary>
/// Bewijst dat de rooster-bouwer (fase 5c) de BKR-indicator ONGEWIJZIGD uit de
/// domein-calculator overneemt, de juiste indicatorkleur bepaalt (tekort/krap/ruim),
/// en de cel-kleuren correct afleidt uit verlof en ziekte.
/// </summary>
public class RoosterBouwerTests
{
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    private static readonly Guid GroepId = Guid.NewGuid();
    private static readonly DateOnly Maandag = new(2026, 6, 15);
    private static readonly DateOnly Baby = new(2025, 11, 1); // ~7 mnd op de peilweek → 0-1 jaar

    private static Kind Kind() => new()
    {
        Voornaam = "K", Achternaam = "L",
        Geboortedatum = Baby, StamgroepId = GroepId,
        Startdatum = new DateOnly(2025, 1, 1),
        Contracttype = Contracttype.Weken49,
        GewensteOpvangdagen = AlleWeekdagen,
    };

    private static Medewerker Medewerker(string achternaam) => new()
    {
        Id = Guid.NewGuid(), Voornaam = "M", Achternaam = achternaam, VasteStamgroepId = GroepId,
    };

    private static WeekplanningDto Weekplanning(int aantalBabys)
    {
        var stamgroep = new Stamgroep { Id = GroepId, Naam = "Bengeltjes", MaxKinderen = 12 };
        stamgroep.Kinderen = Enumerable.Range(0, aantalBabys).Select(_ => Kind()).ToList();
        return WeekplanningBouwer.Bouw(Maandag, new[] { stamgroep }, Array.Empty<Schoolvakantie>());
    }

    private static RoosterDagIndicatorDto MaandagIndicator(RoosterWeekDto dto) =>
        dto.Groepen.Single().Indicatoren.Single(i => i.Datum == Maandag);

    [Fact]
    public void Indicator_NodigPmers_is_gelijk_aan_de_domein_calculator()
    {
        // 4 baby's: verwachte BKR via de calculator zelf.
        WeekplanningDto weekplanning = Weekplanning(4);
        int verwachtNodig = BkrCalculator
            .Bereken(GroepSamenstelling.VanafGeboortedata(Enumerable.Repeat(Baby, 4), Maandag))
            .VereisteHoeveelheidPmers;

        RoosterWeekDto dto = RoosterBouwer.Bouw(
            weekplanning, null, Array.Empty<Roosterdienst>(), Array.Empty<Medewerker>(),
            Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());

        Assert.Equal(verwachtNodig, MaandagIndicator(dto).NodigPmers);
        Assert.Equal(2, verwachtNodig); // ceil(4/3)
    }

    [Fact]
    public void Indicator_is_rood_bij_tekort_en_oranje_bij_precies_genoeg()
    {
        WeekplanningDto weekplanning = Weekplanning(4); // nodig = 2

        // Geen diensten -> ingepland 0 < 2 -> rood.
        RoosterWeekDto leeg = RoosterBouwer.Bouw(
            weekplanning, null, Array.Empty<Roosterdienst>(), Array.Empty<Medewerker>(),
            Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());
        Assert.Equal(0, MaandagIndicator(leeg).IngeplandPmers);
        Assert.Equal(BkrIndicatorKleur.Rood, MaandagIndicator(leeg).Kleur);

        // Twee diensten op maandag -> ingepland 2 == nodig 2 -> oranje (krap).
        Medewerker a = Medewerker("Aaltink"), b = Medewerker("Bakker");
        var diensten = new[]
        {
            new Roosterdienst { Id = Guid.NewGuid(), MedewerkerId = a.Id, StamgroepId = GroepId, Datum = Maandag },
            new Roosterdienst { Id = Guid.NewGuid(), MedewerkerId = b.Id, StamgroepId = GroepId, Datum = Maandag },
        };

        RoosterWeekDto vol = RoosterBouwer.Bouw(
            weekplanning, null, diensten, new[] { a, b },
            Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());
        Assert.Equal(2, MaandagIndicator(vol).IngeplandPmers);
        Assert.Equal(BkrIndicatorKleur.Oranje, MaandagIndicator(vol).Kleur);
    }

    [Fact]
    public void Celkleur_volgt_verlof_en_ziekte()
    {
        WeekplanningDto weekplanning = Weekplanning(0); // kinderen niet relevant voor celkleur
        Medewerker ziekMw = Medewerker("Aaltink");
        Medewerker verlofMw = Medewerker("Bakker");

        var ziek = new[] { new Ziekmelding { MedewerkerId = ziekMw.Id, Begindatum = Maandag, Einddatum = null } };
        var verlof = new[]
        {
            new Verlofaanvraag
            {
                MedewerkerId = verlofMw.Id, Begindatum = Maandag, Einddatum = Maandag,
                Status = VerlofStatus.Goedgekeurd, AantalUren = 8,
            },
        };

        RoosterWeekDto dto = RoosterBouwer.Bouw(
            weekplanning, null, Array.Empty<Roosterdienst>(), new[] { ziekMw, verlofMw }, verlof, ziek);

        RoosterGroepDto groep = dto.Groepen.Single();
        RoosterCelDto ziekCel = groep.Rijen.Single(r => r.MedewerkerId == ziekMw.Id).Cellen.Single(c => c.Datum == Maandag);
        RoosterCelDto verlofCel = groep.Rijen.Single(r => r.MedewerkerId == verlofMw.Id).Cellen.Single(c => c.Datum == Maandag);

        Assert.Equal(RoosterCelKleur.Ziek, ziekCel.Kleur);
        Assert.Equal(RoosterCelKleur.VerlofGoedgekeurd, verlofCel.Kleur);
    }
}
