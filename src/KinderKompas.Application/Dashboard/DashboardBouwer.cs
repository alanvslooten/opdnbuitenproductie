using KinderKompas.Application.Planning;
using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Dashboard;

/// <summary>
/// Assembleert — puur en testbaar — het dashboard-leesmodel uit reeds berekende
/// brongegevens: de weekplanning (met de BKR-uitkomst per groep per dag, uit de
/// rekenkern van fase 2), de verstuur-status van het rooster + de diensten van de dag,
/// en de scalaire widget-cijfers. Geen database, geen nieuwe businessregel — de
/// bezetting-vs-BKR-vergelijking is de enige afleiding en gebeurt hier zichtbaar.
/// </summary>
public static class DashboardBouwer
{
    public static DashboardDto Bouw(
        DateOnly datum,
        WeekplanningDto weekplanning,
        bool roosterVerstuurd,
        IReadOnlyCollection<Roosterdienst> dienstenVandaag,
        DashboardCijfers cijfers)
    {
        ArgumentNullException.ThrowIfNull(weekplanning);
        ArgumentNullException.ThrowIfNull(dienstenVandaag);
        ArgumentNullException.ThrowIfNull(cijfers);

        var groepen = new List<DashboardGroepDto>(weekplanning.Stamgroepen.Count);
        bool isOpvangdag = false;

        foreach (StamgroepWeekDto groep in weekplanning.Stamgroepen)
        {
            DagPlanningDto? dag = groep.Dagen.FirstOrDefault(d => d.Datum == datum);
            if (dag is not null)
            {
                isOpvangdag = true;
            }

            int aantalKinderen = dag?.Kinderen.Count ?? 0;
            int? vereiste = dag?.Bkr.VereisteHoeveelheidPmers;
            bool bovenMax = dag?.Bkr.Overschrijdt ?? false;

            // Eén medewerker telt één keer mee, ook al staat hij in meerdere diensten.
            int ingepland = dienstenVandaag
                .Where(d => d.StamgroepId == groep.StamgroepId)
                .Select(d => d.MedewerkerId)
                .Distinct()
                .Count();

            bool onderbezet = roosterVerstuurd && vereiste is { } v && ingepland < v;

            groepen.Add(new DashboardGroepDto(
                groep.StamgroepId, groep.Naam, aantalKinderen, vereiste, ingepland, bovenMax, onderbezet));
        }

        int totaalKinderen = groepen.Sum(g => g.AantalKinderen);
        int totaalMedewerkers = dienstenVandaag.Select(d => d.MedewerkerId).Distinct().Count();

        var badge = new BkrBadgeDto(
            isOpvangdag,
            Overschrijding: groepen.Any(g => g.BovenMaximum || g.Onderbezet),
            AantalGroepenInOrde: groepen.Count(g => !g.BovenMaximum && !g.Onderbezet),
            AantalGroepen: groepen.Count);

        return new DashboardDto(
            datum,
            isOpvangdag,
            totaalKinderen,
            roosterVerstuurd,
            totaalMedewerkers,
            cijfers.AantalKinderenBinnenkortVier,
            badge,
            groepen,
            new WachtlijstWidgetDto(cijfers.AantalWachtend),
            new ObservatiesWidgetDto(cijfers.ObservatiesOverschreden, cijfers.ObservatiesBinnenkort),
            new ActiecentrumWidgetDto(cijfers.OpenToDos, cijfers.OngelezenMeldingen),
            cijfers.RecenteActiviteit);
    }
}
