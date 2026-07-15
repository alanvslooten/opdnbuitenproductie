using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Exceptions;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Planning;

/// <summary>
/// Bouwt de weekplanning-weergavedata uit reeds geladen domeingegevens. Pure functie
/// zonder database- of UI-afhankelijkheid: de controller laadt stamgroepen (met hun
/// kinderen), de schoolvakanties en de dagafwijkingen en geeft die hier door. De
/// aanwezigheids- en BKR-logica komt volledig uit het domein (<see cref="Dagindeling"/>
/// bovenop <see cref="Aanwezigheid"/> + <see cref="BkrCalculator"/>); hier zit géén
/// nieuwe businessregel.
///
/// De telling per groep per dag loopt via <see cref="Dagindeling"/>, zodat dagafwijkingen
/// (ruildag, incidenteel op een andere groep, extra dag, afwezig) de groepsgrootte en de
/// BKR beïnvloeden. Zonder afwijkingen is het resultaat identiek aan het oude gedrag.
/// </summary>
public static class WeekplanningBouwer
{
    private const int OpvangdagenPerWeek = 5; // maandag t/m vrijdag

    /// <summary>De maandag van de week waarin de gegeven datum valt.</summary>
    public static DateOnly WeekBeginVan(DateOnly datum)
    {
        int offset = ((int)datum.DayOfWeek + 6) % 7; // ma=0, di=1, ... zo=6
        return datum.AddDays(-offset);
    }

    public static WeekplanningDto Bouw(
        DateOnly enigeDatumInWeek,
        IEnumerable<Stamgroep> stamgroepen,
        IEnumerable<Schoolvakantie> vakanties,
        IEnumerable<Roosterdienst>? diensten = null,
        IEnumerable<Dagplaatsing>? dagplaatsingen = null)
    {
        ArgumentNullException.ThrowIfNull(stamgroepen);
        ArgumentNullException.ThrowIfNull(vakanties);

        DateOnly weekBegin = WeekBeginVan(enigeDatumInWeek);
        IReadOnlyList<Stamgroep> groepLijst =
            stamgroepen as IReadOnlyList<Stamgroep> ?? stamgroepen.ToList();
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();
        IReadOnlyList<Roosterdienst> dienstLijst =
            diensten as IReadOnlyList<Roosterdienst> ?? diensten?.ToList() ?? [];
        IReadOnlyList<Dagplaatsing> afwijkingen =
            dagplaatsingen as IReadOnlyList<Dagplaatsing> ?? dagplaatsingen?.ToList() ?? [];

        // Een dagafwijking kan een kind op een andere dan zijn thuisgroep zetten, dus de
        // telling kijkt over ALLE geladen kinderen (elk kind hoort bij één thuisgroep).
        IReadOnlyList<Kind> alleKinderen = groepLijst.SelectMany(s => s.Kinderen).ToList();

        var groepen = new List<StamgroepWeekDto>();
        foreach (Stamgroep stamgroep in groepLijst)
        {
            var dagen = new List<DagPlanningDto>(OpvangdagenPerWeek);
            for (int i = 0; i < OpvangdagenPerWeek; i++)
            {
                DateOnly datum = weekBegin.AddDays(i);
                dagen.Add(BouwDag(datum, stamgroep.Id, alleKinderen, vakantieLijst, dienstLijst, afwijkingen));
            }

            groepen.Add(new StamgroepWeekDto(
                stamgroep.Id, stamgroep.Naam, stamgroep.MaxKinderen, dagen));
        }

        return new WeekplanningDto(weekBegin, groepen);
    }

    private static DagPlanningDto BouwDag(
        DateOnly datum, Guid stamgroepId, IReadOnlyList<Kind> alleKinderen,
        IReadOnlyList<Schoolvakantie> vakanties, IReadOnlyList<Roosterdienst> diensten,
        IReadOnlyList<Dagplaatsing> afwijkingen)
    {
        IReadOnlyList<Kind> aanwezig =
            Dagindeling.OpGroepOpDag(alleKinderen, stamgroepId, datum, afwijkingen, vakanties);

        var kindDtos = aanwezig
            .Select(k => new AanwezigKindDto(
                k.Id, k.Voornaam, k.Achternaam, k.StamgroepId,
                k.LeeftijdscategorieOp(datum).Groep, k.Contracttype))
            .ToList();

        var samenstelling = GroepSamenstelling.VanafGeboortedata(
            aanwezig.Select(k => k.Geboortedatum), datum);

        BkrDagDto bkr;
        try
        {
            BkrUitkomst uitkomst = BkrCalculator.Bereken(samenstelling);
            bkr = new BkrDagDto(samenstelling.Totaal, uitkomst.VereisteHoeveelheidPmers, false, null);
        }
        catch (GroepOverschrijdtMaximumException ex)
        {
            // Overplanning: de groep zit boven het wettelijk maximum. Geen harde fout
            // in een planningsweergave — toon het als signaal aan de planner.
            bkr = new BkrDagDto(samenstelling.Totaal, null, true, ex.Message);
        }

        var begeleiders = diensten
            .Where(d => d.StamgroepId == stamgroepId && d.Datum == datum)
            .Select(d => new PlanningBegeleiderDto(
                d.MedewerkerId,
                d.Medewerker is null ? "" : $"{d.Medewerker.Voornaam} {d.Medewerker.Achternaam}",
                d.Taakomschrijving))
            .OrderBy(b => b.Naam)
            .ToList();

        return new DagPlanningDto(
            datum,
            Aanwezigheid.NaarWeekdag(datum),
            Aanwezigheid.IsSchoolvakantie(datum, vakanties),
            kindDtos,
            bkr,
            begeleiders);
    }
}
