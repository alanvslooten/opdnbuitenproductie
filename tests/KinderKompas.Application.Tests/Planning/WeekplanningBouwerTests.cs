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

    private static Kind KindMetId(Guid id, DateOnly geboortedatum, Guid stamgroepId)
    {
        Kind k = Kind(geboortedatum, stamgroepId);
        k.Id = id;
        return k;
    }

    private static DagPlanningDto DagVan(WeekplanningDto week, Guid groepId, DateOnly datum) =>
        week.Stamgroepen.Single(g => g.StamgroepId == groepId).Dagen.Single(d => d.Datum == datum);

    [Fact]
    public void Bouw_MetIncidenteleDagafwijking_VerplaatstKindTussenGroepen()
    {
        Guid groepA = Guid.NewGuid();
        Guid groepB = Guid.NewGuid();
        var a = new Stamgroep { Id = groepA, Naam = "Bengeltjes", MaxKinderen = 12 };
        var b = new Stamgroep { Id = groepB, Naam = "Boefjes", MaxKinderen = 12 };
        DateOnly peuter = new(2024, 1, 1);

        Guid flexKindId = Guid.NewGuid();
        a.Kinderen = new List<Kind> { KindMetId(Guid.NewGuid(), peuter, groepA), KindMetId(flexKindId, peuter, groepA) };
        b.Kinderen = new List<Kind> { KindMetId(Guid.NewGuid(), peuter, groepB) };

        DateOnly woensdag = new(2026, 3, 4);
        // Flex-kind staat op woensdag incidenteel op groep B i.p.v. zijn thuisgroep A.
        var afwijking = new Dagplaatsing
        {
            KindId = flexKindId, Datum = woensdag, StamgroepId = groepB, Soort = DagplaatsingSoort.Incidenteel,
        };

        WeekplanningDto week = WeekplanningBouwer.Bouw(
            woensdag, new[] { a, b }, Array.Empty<Schoolvakantie>(),
            diensten: null, dagplaatsingen: new[] { afwijking });

        // Woensdag: A verliest het flex-kind (2 → 1), B krijgt het erbij (1 → 2).
        Assert.Equal(1, DagVan(week, groepA, woensdag).Bkr.AantalKinderen);
        Assert.Equal(2, DagVan(week, groepB, woensdag).Bkr.AantalKinderen);

        // Donderdag (geen afwijking): het kind staat gewoon weer op zijn thuisgroep A.
        DateOnly donderdag = new(2026, 3, 5);
        Assert.Equal(2, DagVan(week, groepA, donderdag).Bkr.AantalKinderen);
        Assert.Equal(1, DagVan(week, groepB, donderdag).Bkr.AantalKinderen);
    }

    [Fact]
    public void Bouw_MetAfwezigheidsafwijking_HaaltKindUitDeTellingEnBkr()
    {
        Guid groepId = Guid.NewGuid();
        var stamgroep = new Stamgroep { Id = groepId, Naam = "Bengeltjes", MaxKinderen = 12 };
        DateOnly baby = new(2025, 11, 1);
        Guid afwezigKindId = Guid.NewGuid();
        // Vier baby's (BKR: ceil(4/3) = 2 pm'ers). Eén is woensdag afwezig → 3 baby's = 1 pm'er.
        stamgroep.Kinderen = new List<Kind>
        {
            KindMetId(Guid.NewGuid(), baby, groepId),
            KindMetId(Guid.NewGuid(), baby, groepId),
            KindMetId(Guid.NewGuid(), baby, groepId),
            KindMetId(afwezigKindId, baby, groepId),
        };

        DateOnly woensdag = new(2026, 3, 4);
        var afwezig = new Dagplaatsing
        {
            KindId = afwezigKindId, Datum = woensdag, StamgroepId = null, Soort = DagplaatsingSoort.Afwezig,
        };

        WeekplanningDto week = WeekplanningBouwer.Bouw(
            woensdag, new[] { stamgroep }, Array.Empty<Schoolvakantie>(),
            diensten: null, dagplaatsingen: new[] { afwezig });

        DagPlanningDto wo = DagVan(week, groepId, woensdag);
        Assert.Equal(3, wo.Bkr.AantalKinderen);
        Assert.Equal(1, wo.Bkr.VereisteHoeveelheidPmers);

        // Dinsdag (geen afwijking): alle vier aanwezig, 2 pm'ers.
        DagPlanningDto di = DagVan(week, groepId, new DateOnly(2026, 3, 3));
        Assert.Equal(4, di.Bkr.AantalKinderen);
        Assert.Equal(2, di.Bkr.VereisteHoeveelheidPmers);
    }
}
