namespace KinderKompas.Application.Contacten;

/// <summary>Samenvatting van een contact voor de contactenlijst.</summary>
public sealed record ContactDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    string VolledigeNaam,
    string? Telefoon,
    string? Email,
    bool IsIntern,
    string? Aantekeningen,
    int AantalRondleidingen,
    int AantalInschrijvingen,
    int AantalGeplaatsteKinderen);

/// <summary>Een rondleiding in de contacthistorie.</summary>
public sealed record RondleidingDto(Guid Id, DateOnly Datum, int Status, string? Notitie);

/// <summary>Een wachtlijst-inschrijving die bij het contact hoort (met voorstel-telling).</summary>
public sealed record ContactInschrijvingDto(
    Guid Id,
    string KindNaam,
    DateOnly GewensteStartdatum,
    int Status,
    int AantalVoorstellen);

/// <summary>Een geplaatst kind dat bij het contact hoort.</summary>
public sealed record ContactKindDto(Guid Id, string Naam, string StamgroepNaam);

/// <summary>Eén regel uit het wijzigingslogboek van een contact.</summary>
public sealed record ContactLogregelDto(DateTime Tijdstip, string Omschrijving);

/// <summary>Volledig contactdossier met historie: rondleidingen, inschrijvingen, geplaatste kinderen, logboek.</summary>
public sealed record ContactDetailDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    string? Telefoon,
    string? Email,
    bool IsIntern,
    string? Aantekeningen,
    IReadOnlyList<RondleidingDto> Rondleidingen,
    IReadOnlyList<ContactInschrijvingDto> Inschrijvingen,
    IReadOnlyList<ContactKindDto> GeplaatsteKinderen,
    IReadOnlyList<ContactLogregelDto> Logboek);

/// <summary>Invoer voor het aanmaken/bewerken van een contact.</summary>
public sealed record ContactInvoer(
    string Voornaam,
    string Achternaam,
    string? Telefoon,
    string? Email,
    bool IsIntern,
    string? Aantekeningen);

/// <summary>Invoer voor een rondleiding bij een contact.</summary>
public sealed record RondleidingInvoer(DateOnly Datum, int Status, string? Notitie);
