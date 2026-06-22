using KinderKompas.Application.Dashboard;
using KinderKompas.Application.Planning;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Dashboard;

/// <summary>
/// Bewijst de dashboard-assemblage (fase 9b): totalen worden gesommeerd over de groepen,
/// de BKR-badge slaat aan bij een groep boven het maximum óf onderbezetting van een
/// verstuurd rooster, ingeplande pm'ers tellen per medewerker uniek, en op een dag
/// zonder opvang (weekend) is er geen opvangdag.
/// </summary>
public class DashboardBouwerTests
{
    private static readonly Guid GroepA = Guid.NewGuid();
    private static readonly Guid GroepB = Guid.NewGuid();
    private static readonly DateOnly Maandag = new(2026, 6, 15);

    private static DagPlanningDto Dag(DateOnly datum, Guid groep, int aantalKinderen, int? vereiste, bool bovenMax)
    {
        var kinderen = Enumerable.Range(0, aantalKinderen)
            .Select(_ => new AanwezigKindDto(
                Guid.NewGuid(), "K", "ind", groep, Leeftijdsgroep.EenTotTwee, Contracttype.Weken49))
            .ToList();
        return new DagPlanningDto(datum, Weekdag.Maandag, false, kinderen,
            new BkrDagDto(aantalKinderen, vereiste, bovenMax, bovenMax ? "boven max" : null),
            Array.Empty<PlanningBegeleiderDto>());
    }

    private static DashboardCijfers LegeCijfers =>
        new(0, 0, 0, 0, 0, 0, Array.Empty<ActiviteitDto>());

    private static Roosterdienst Dienst(Guid medewerker, Guid groep, DateOnly datum) =>
        new() { MedewerkerId = medewerker, StamgroepId = groep, Datum = datum };

    [Fact]
    public void Sommeert_kinderen_en_telt_medewerkers_uniek()
    {
        var weekplanning = new WeekplanningDto(Maandag, new[]
        {
            new StamgroepWeekDto(GroepA, "Boefjes", 12, new[] { Dag(Maandag, GroepA, 8, 2, false) }),
            new StamgroepWeekDto(GroepB, "Bengeltjes", 12, new[] { Dag(Maandag, GroepB, 4, 1, false) }),
        });

        Guid mw1 = Guid.NewGuid(), mw2 = Guid.NewGuid();
        var diensten = new[]
        {
            Dienst(mw1, GroepA, Maandag),
            Dienst(mw2, GroepA, Maandag),
            Dienst(mw1, GroepB, Maandag), // mw1 ook in B — totaal blijft 2 unieke medewerkers
        };

        DashboardDto dto = DashboardBouwer.Bouw(Maandag, weekplanning, roosterVerstuurd: true, diensten, LegeCijfers);

        Assert.True(dto.IsOpvangdag);
        Assert.Equal(12, dto.TotaalKinderenVandaag);
        Assert.Equal(2, dto.TotaalMedewerkersVandaag);

        DashboardGroepDto a = dto.Groepen.Single(g => g.StamgroepId == GroepA);
        Assert.Equal(2, a.IngeplandePmers);   // mw1 + mw2, niet dubbel
        Assert.False(a.Onderbezet);           // 2 ingepland >= 2 vereist
    }

    [Fact]
    public void Badge_slaat_aan_bij_onderbezetting_van_verstuurd_rooster()
    {
        var weekplanning = new WeekplanningDto(Maandag, new[]
        {
            new StamgroepWeekDto(GroepA, "Boefjes", 12, new[] { Dag(Maandag, GroepA, 12, 3, false) }),
        });
        // Verstuurd rooster, maar slechts 1 pm'er ingepland tegen 3 vereist.
        var diensten = new[] { Dienst(Guid.NewGuid(), GroepA, Maandag) };

        DashboardDto dto = DashboardBouwer.Bouw(Maandag, weekplanning, roosterVerstuurd: true, diensten, LegeCijfers);

        Assert.True(dto.Bkr.Overschrijding);
        Assert.True(dto.Groepen.Single().Onderbezet);
        Assert.Equal(0, dto.Bkr.AantalGroepenInOrde);
    }

    [Fact]
    public void Conceptrooster_telt_niet_als_onderbezet()
    {
        var weekplanning = new WeekplanningDto(Maandag, new[]
        {
            new StamgroepWeekDto(GroepA, "Boefjes", 12, new[] { Dag(Maandag, GroepA, 12, 3, false) }),
        });

        // Rooster nog niet verstuurd → onderbezetting is nog geen signaal.
        DashboardDto dto = DashboardBouwer.Bouw(
            Maandag, weekplanning, roosterVerstuurd: false, Array.Empty<Roosterdienst>(), LegeCijfers);

        Assert.False(dto.Bkr.Overschrijding);
        Assert.False(dto.Groepen.Single().Onderbezet);
    }

    [Fact]
    public void Groep_boven_maximum_zet_de_badge_aan_ook_zonder_rooster()
    {
        var weekplanning = new WeekplanningDto(Maandag, new[]
        {
            new StamgroepWeekDto(GroepA, "Boefjes", 12, new[] { Dag(Maandag, GroepA, 20, null, bovenMax: true) }),
        });

        DashboardDto dto = DashboardBouwer.Bouw(
            Maandag, weekplanning, roosterVerstuurd: false, Array.Empty<Roosterdienst>(), LegeCijfers);

        Assert.True(dto.Bkr.Overschrijding);
        Assert.True(dto.Groepen.Single().BovenMaximum);
    }

    [Fact]
    public void Weekend_is_geen_opvangdag()
    {
        DateOnly zaterdag = Maandag.AddDays(5);
        var weekplanning = new WeekplanningDto(Maandag, new[]
        {
            new StamgroepWeekDto(GroepA, "Boefjes", 12, new[] { Dag(Maandag, GroepA, 8, 2, false) }),
        });

        DashboardDto dto = DashboardBouwer.Bouw(
            zaterdag, weekplanning, roosterVerstuurd: true, Array.Empty<Roosterdienst>(), LegeCijfers);

        Assert.False(dto.IsOpvangdag);
        Assert.False(dto.Bkr.IsOpvangdag);
        Assert.Equal(0, dto.TotaalKinderenVandaag);
    }

    [Fact]
    public void Scalaire_widgetcijfers_worden_doorgegeven()
    {
        var weekplanning = new WeekplanningDto(Maandag, Array.Empty<StamgroepWeekDto>());
        var activiteit = new[] { new ActiviteitDto(Guid.NewGuid(), MeldingSoort.Verlofaanvraag, "T", "x", DateTime.UtcNow) };
        var cijfers = new DashboardCijfers(5, 2, 3, 4, 6, 7, activiteit);

        DashboardDto dto = DashboardBouwer.Bouw(
            Maandag, weekplanning, roosterVerstuurd: false, Array.Empty<Roosterdienst>(), cijfers);

        Assert.Equal(5, dto.Wachtlijst.AantalWachtend);
        Assert.Equal(2, dto.AantalKinderenBinnenkortVier);
        Assert.Equal(3, dto.Observaties.Overschreden);
        Assert.Equal(4, dto.Observaties.Binnenkort);
        Assert.Equal(6, dto.Actiecentrum.OpenToDos);
        Assert.Equal(7, dto.Actiecentrum.OngelezenMeldingen);
        Assert.Single(dto.RecenteActiviteit);
    }
}
