using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Planning;

/// <summary>
/// De weekplanning: per stamgroep, per opvangdag (ma-vr) welke kinderen aanwezig
/// zijn en de bijbehorende BKR-uitkomst. Afgeleide weergavedata — er wordt niets
/// opgeslagen; het is de input voor het rooster (fase 5).
/// </summary>
public sealed record WeekplanningDto(
    DateOnly WeekBegin,
    IReadOnlyList<StamgroepWeekDto> Stamgroepen);

/// <summary>De maandplanning (alleen-lezen): alle weken die de maand raken.</summary>
public sealed record MaandPlanningDto(
    int Jaar,
    int Maand,
    IReadOnlyList<WeekplanningDto> Weken);

public sealed record StamgroepWeekDto(
    Guid StamgroepId,
    string Naam,
    int MaxKinderen,
    IReadOnlyList<DagPlanningDto> Dagen);

public sealed record DagPlanningDto(
    DateOnly Datum,
    Weekdag Dag,
    bool IsSchoolvakantie,
    IReadOnlyList<AanwezigKindDto> Kinderen,
    BkrDagDto Bkr,
    IReadOnlyList<PlanningBegeleiderDto> Begeleiders);

/// <summary>Een ingeplande begeleider (medewerker) op een dag, uit het verstuurde rooster.</summary>
public sealed record PlanningBegeleiderDto(Guid MedewerkerId, string Naam, string? Taakomschrijving);

/// <summary>
/// Antwoord van het dagfilter: de aanwezige kinderen én de ingeplande begeleiders
/// (uit het verstuurde rooster) op die dag.
/// </summary>
public sealed record DagFilterDto(
    IReadOnlyList<AanwezigKindDto> Kinderen,
    IReadOnlyList<PlanningBegeleiderDto> Begeleiders);

/// <summary>Een aanwezig kind in de planning. Bevat bewust GEEN privacygevoelige oudergegevens.</summary>
public sealed record AanwezigKindDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    Guid StamgroepId,
    Leeftijdsgroep Leeftijdsgroep,
    Contracttype Contracttype);

/// <summary>
/// De BKR-status van een dag. Bij overplanning (groep boven het wettelijk maximum)
/// is <see cref="VereisteHoeveelheidPmers"/> null en is <see cref="Overschrijdt"/>
/// waar, met een toelichting in <see cref="Melding"/> — i.p.v. een harde fout.
/// </summary>
public sealed record BkrDagDto(
    int AantalKinderen,
    int? VereisteHoeveelheidPmers,
    bool Overschrijdt,
    string? Melding);
