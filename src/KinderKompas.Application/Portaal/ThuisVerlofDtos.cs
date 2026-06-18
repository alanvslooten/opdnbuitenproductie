using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Portaal;

/// <summary>
/// Verlofaanvraag-invoer voor het thuis-portaal: identiek aan de planner-invoer
/// maar ZONDER medewerker — die wordt server-side op de ingelogde medewerker gezet,
/// zodat een medewerker nooit verlof voor een ander kan aanvragen.
/// </summary>
public sealed record ThuisVerlofInvoer(
    DateOnly Begindatum,
    DateOnly Einddatum,
    decimal AantalUren,
    VerlofCategorie Categorie,
    string? Reden);
