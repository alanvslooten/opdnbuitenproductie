using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Exceptions;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Planning;

/// <summary>
/// Bouwt de weekplanning-weergavedata uit reeds geladen domeingegevens. Pure functie
/// zonder database- of UI-afhankelijkheid: de controller laadt stamgroepen (met hun
/// kinderen) en de schoolvakanties en geeft die hier door. De aanwezigheids- en
/// BKR-logica komt volledig uit het domein (<see cref="Aanwezigheid"/> +
/// <see cref="BkrCalculator"/>); hier zit géén nieuwe businessregel.
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
        IEnumerable<Roosterdienst>? diensten = null)
    {
        ArgumentNullException.ThrowIfNull(stamgroepen);
        ArgumentNullException.ThrowIfNull(vakanties);

        DateOnly weekBegin = WeekBeginVan(enigeDatumInWeek);
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();
        IReadOnlyList<Roosterdienst> dienstLijst =
            diensten as IReadOnlyList<Roosterdienst> ?? diensten?.ToList() ?? [];

        var groepen = new List<StamgroepWeekDto>();
        foreach (Stamgroep stamgroep in stamgroepen)
        {
            var dagen = new List<DagPlanningDto>(OpvangdagenPerWeek);
            for (int i = 0; i < OpvangdagenPerWeek; i++)
            {
                DateOnly datum = weekBegin.AddDays(i);
                dagen.Add(BouwDag(datum, stamgroep.Id, stamgroep.Kinderen, vakantieLijst, dienstLijst));
            }

            groepen.Add(new StamgroepWeekDto(
                stamgroep.Id, stamgroep.Naam, stamgroep.MaxKinderen, dagen));
        }

        return new WeekplanningDto(weekBegin, groepen);
    }

    private static DagPlanningDto BouwDag(
        DateOnly datum, Guid stamgroepId, IEnumerable<Kind> kinderen,
        IReadOnlyList<Schoolvakantie> vakanties, IReadOnlyList<Roosterdienst> diensten)
    {
        IReadOnlyList<Kind> aanwezig = Aanwezigheid.AanwezigOp(kinderen, datum, vakanties);

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
