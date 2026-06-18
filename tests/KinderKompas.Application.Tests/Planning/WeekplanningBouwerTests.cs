using KinderKompas.Application.Planning;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Planning;

/// <summary>
/// Bewijst dat de weekplanning-bouwer de aanwezigheids- en BKR-logica uit het
/// domein correct samenstelt tot weergavedata: vijf opvangdagen per stamgroep,
/// vakantie-markering, BKR per dag, en overplanning als nette status i.p.v. een fout.
/// </summary>
public class WeekplanningBouwerTests
{
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    private static Kind Kind(DateOnly geboortedatum, Guid stamgroepId, Contracttype type = Contracttype.Weken49) => new()
    {
        Voornaam = "K", Achternaam = "L",
        Geboortedatum = geboortedatum,
        StamgroepId = stamgroepId,
        Startdatum = new DateOnly(2025, 1, 1),
        Contracttype = type,
        GewensteOpvangdagen = AlleWeekdagen,
    };

    [Fact]
    public void Bouw_GeeftVijfDagen_MetVakantieEnBkrPerDag()
    {
        Guid groepId = Guid.NewGuid();
        var stamgroep = new Stamgroep { Id = groepId, Naam = "Bengeltjes", MaxKinderen = 12 };
        // Vier baby's (0-1 jaar op de peilweek) → BKR: ceil(4/3) = 2 pm'ers.
        DateOnly baby = new(2025, 11, 1);
        stamgroep.Kinderen = new List<Kind>
        {
            Kind(baby, groepId), Kind(baby, groepId), Kind(baby, groepId), Kind(baby, groepId),
        };

        var vakantie = new Schoolvakantie
        {
            Naam = "Zomervakantie", Schooljaar = 2025,
            Begindatum = new DateOnly(2026, 7, 13), Einddatum = new DateOnly(2026, 8, 23),
        };

        // Een dag midden in de vakantie; de bouwer normaliseert naar de maandag.
        WeekplanningDto week = WeekplanningBouwer.Bouw(
            new DateOnly(2026, 8, 5), new[] { stamgroep }, new[] { vakantie });

        Assert.Equal(new DateOnly(2026, 8, 3), week.WeekBegin); // maandag
        StamgroepWeekDto groep = Assert.Single(week.Stamgroepen);
        Assert.Equal(5, groep.Dagen.Count);
        Assert.All(groep.Dagen, d => Assert.True(d.IsSchoolvakantie));

        DagPlanningDto maandag = groep.Dagen[0];
        Assert.Equal(Weekdag.Maandag, maandag.Dag);
        // 49-wekenkinderen lopen door in de vakantie: 4 baby's aanwezig, 2 pm'ers vereist.
        Assert.Equal(4, maandag.Bkr.AantalKinderen);
        Assert.Equal(2, maandag.Bkr.VereisteHoeveelheidPmers);
        Assert.False(maandag.Bkr.Overschrijdt);
    }

    [Fact]
    public void Bouw_BijOverplanning_GeeftOverschrijdingZonderFout()
    {
        Guid groepId = Guid.NewGuid();
        var stamgroep = new Stamgroep { Id = groepId, Naam = "Boefjes", MaxKinderen = 12 };
        // 13 baby's: ver boven het wettelijk maximum (12) van een 0-1 groep.
        DateOnly baby = new(2025, 11, 1);
        stamgroep.Kinderen = Enumerable.Range(0, 13).Select(_ => Kind(baby, groepId)).ToList();

        WeekplanningDto week = WeekplanningBouwer.Bouw(
            new DateOnly(2026, 9, 7), new[] { stamgroep }, Array.Empty<Schoolvakantie>());

        DagPlanningDto maandag = week.Stamgroepen[0].Dagen[0];
        Assert.Equal(13, maandag.Bkr.AantalKinderen);
        Assert.True(maandag.Bkr.Overschrijdt);
        Assert.Null(maandag.Bkr.VereisteHoeveelheidPmers);
        Assert.NotNull(maandag.Bkr.Melding);
    }
}
