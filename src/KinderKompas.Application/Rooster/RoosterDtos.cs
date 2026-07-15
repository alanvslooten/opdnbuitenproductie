using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Rooster;

/// <summary>Eén regel in de log van verstuurde roosters.</summary>
public sealed record VerstuurdRoosterDto(
    Guid Id,
    DateOnly WeekBegin,
    DateTime? VerstuurdOp,
    int AantalDiensten);

/// <summary>De kleur van de BKR-indicator boven een roosterdag.</summary>
public enum BkrIndicatorKleur
{
    /// <summary>Voldoende bezetting (ingepland ≥ nodig, met marge of geen kinderen).</summary>
    Groen = 0,

    /// <summary>Precies op de grens (ingepland == nodig): voldoet, maar zonder marge.</summary>
    Oranje = 1,

    /// <summary>Tekort (ingepland &lt; nodig) of overplanning (groep boven wettelijk maximum).</summary>
    Rood = 2
}

/// <summary>
/// De kleur van één roostercel voor een medewerker op een dag. Spiegelt het bouwplan:
/// blauw=standaard dienst, oranje=verlof aangevraagd, groen=verlof goedgekeurd, rood=ziek.
/// Een afgekeurde verlofaanvraag verdwijnt (telt niet als kleur).
/// </summary>
public enum RoosterCelKleur
{
    Leeg = 0,
    Standaard = 1,
    VerlofAangevraagd = 2,
    VerlofGoedgekeurd = 3,
    Ziek = 4
}

/// <summary>Het volledige weekrooster: per stamgroep de BKR-indicatoren en de medewerkerrijen.</summary>
public sealed record RoosterWeekDto(
    DateOnly WeekBegin,
    bool Bestaat,
    Guid? RoosterweekId,
    RoosterStatus? Status,
    DateTime? VerstuurdOp,
    IReadOnlyList<RoosterGroepDto> Groepen);

public sealed record RoosterGroepDto(
    Guid StamgroepId,
    string Naam,
    IReadOnlyList<RoosterDagIndicatorDto> Indicatoren,
    IReadOnlyList<RoosterMedewerkerRijDto> Rijen);

/// <summary>De BKR-indicator boven één dag: nodig vs ingepland, met kleur.</summary>
public sealed record RoosterDagIndicatorDto(
    DateOnly Datum,
    Weekdag Dag,
    int AantalKinderen,
    int? NodigPmers,
    int IngeplandPmers,
    bool Overschrijdt,
    BkrIndicatorKleur Kleur);

public sealed record RoosterMedewerkerRijDto(
    Guid MedewerkerId,
    string Naam,
    IReadOnlyList<RoosterCelDto> Cellen);

public sealed record RoosterCelDto(
    DateOnly Datum,
    Weekdag Dag,
    RoosterCelKleur Kleur,
    Guid? DienstId,
    string? Taakomschrijving,
    int UrencorrectieKwartieren,
    Dienstsoort Dienstsoort,
    TimeOnly? Begintijd,
    TimeOnly? Eindtijd,
    decimal? GeplandeUren);

/// <summary>
/// Invoer voor het bijwerken van een dienst (taak, urencorrectie in kwartieren, dienstsoort,
/// en optioneel afwijkende begin-/eindtijd; null = de standaardtijd van de dienstsoort).
/// </summary>
public sealed record DienstInvoer(
    string? Taakomschrijving,
    int UrencorrectieKwartieren,
    Dienstsoort Dienstsoort = Dienstsoort.Regulier,
    TimeOnly? Begintijd = null,
    TimeOnly? Eindtijd = null);

/// <summary>Invoer voor het handmatig toevoegen van een dienst aan de roosterweek.</summary>
public sealed record DienstToevoegenInvoer(Guid MedewerkerId, Guid StamgroepId, DateOnly Datum);
