using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Dashboard;

/// <summary>De bezetting + BKR-stand van één stamgroep op de peildag.</summary>
public sealed record DashboardGroepDto(
    Guid StamgroepId,
    string Naam,
    int AantalKinderen,
    int? VereistePmers,
    int IngeplandePmers,
    bool BovenMaximum,
    bool Onderbezet);

/// <summary>
/// De BKR-badge: de samengevatte bewakingsstand over alle groepen op de peildag.
/// <see cref="Overschrijding"/> is waar zodra één groep boven het wettelijk maximum
/// zit óf (bij een verstuurd rooster) onderbezet is t.o.v. de vereiste pm'ers.
/// </summary>
public sealed record BkrBadgeDto(
    bool IsOpvangdag,
    bool Overschrijding,
    int AantalGroepenInOrde,
    int AantalGroepen);

/// <summary>Wachtlijst-widget: hoeveel kinderen staan te wachten.</summary>
public sealed record WachtlijstWidgetDto(int AantalWachtend);

/// <summary>Observaties-widget: openstaande (overschreden) en aankomende mijlpalen, over alle kinderen.</summary>
public sealed record ObservatiesWidgetDto(int Overschreden, int Binnenkort);

/// <summary>Actiecentrum-widget: de tellers uit het meldingen/to-do-spoor (fase 9a).</summary>
public sealed record ActiecentrumWidgetDto(int OpenToDos, int OngelezenMeldingen);

/// <summary>Eén regel in "recente activiteit" — afgeleid uit de meldingenstroom (fase 9a).</summary>
public sealed record ActiviteitDto(
    Guid Id,
    MeldingSoort Soort,
    string Titel,
    string Tekst,
    DateTime Op);

/// <summary>
/// De scalaire widget-cijfers die de controller uit de losse modules verzamelt en aan
/// de <see cref="DashboardBouwer"/> doorgeeft. Houdt de bouwer puur: hij telt en assembleert,
/// maar laadt zelf niets.
/// </summary>
public sealed record DashboardCijfers(
    int AantalWachtend,
    int AantalKinderenBinnenkortVier,
    int ObservatiesOverschreden,
    int ObservatiesBinnenkort,
    int OpenToDos,
    int OngelezenMeldingen,
    IReadOnlyList<ActiviteitDto> RecenteActiviteit);

/// <summary>
/// Het volledige dashboard-leesmodel: alle cijfers komen live uit de echte modules
/// (BKR-rekenkern, planning, verstuurd rooster, wachtlijst, observaties, actiecentrum).
/// Er zitten géén hardcoded of gemockte waarden in.
/// </summary>
public sealed record DashboardDto(
    DateOnly Datum,
    bool IsOpvangdag,
    int TotaalKinderenVandaag,
    bool RoosterVerstuurd,
    int TotaalMedewerkersVandaag,
    int AantalKinderenBinnenkortVier,
    BkrBadgeDto Bkr,
    IReadOnlyList<DashboardGroepDto> Groepen,
    WachtlijstWidgetDto Wachtlijst,
    ObservatiesWidgetDto Observaties,
    ActiecentrumWidgetDto Actiecentrum,
    IReadOnlyList<ActiviteitDto> RecenteActiviteit);
