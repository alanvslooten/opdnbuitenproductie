namespace KinderKompas.Application.Abstractions;

/// <summary>
/// De ingelogde gebruiker van de huidige request, afgeleid uit de JWT-claims.
/// Business-logica (zoals de privacy-projectie van oudergegevens) vraagt rechten
/// ALTIJD via deze abstractie op en leest nooit zelf de HttpContext of claims.
/// De implementatie leeft in de Api-laag (heeft toegang tot de HttpContext).
/// </summary>
public interface ICurrentUser
{
    bool IsGeauthenticeerd { get; }

    /// <summary>Identity-gebruikers-id (claim "sub"), of null bij anoniem.</summary>
    string? UserId { get; }

    /// <summary>Organisatie (tenant) uit de claim, of null bij anoniem.</summary>
    Guid? OrganisatieId { get; }

    /// <summary>
    /// De gekoppelde medewerker uit de claim, of null voor een gedeeld portaal-account
    /// of een anonieme aanroeper. Stuurt o.a. de zichtbaarheid van observaties
    /// (een medewerker ziet alleen de kinderen waarvan hij mentor is).
    /// </summary>
    Guid? MedewerkerId { get; }

    /// <summary>
    /// De stamgroep waaraan een Groepsportaal-account vast hangt, of null voor een
    /// persoonlijk/back-office account. Scopt alle groepsportaal-data tot die groep.
    /// </summary>
    Guid? StamgroepId { get; }

    /// <summary>De capabilities (rechten) die in de token zijn opgenomen.</summary>
    IReadOnlySet<string> Capabilities { get; }

    /// <summary>True als de gebruiker de gegeven capability bezit.</summary>
    bool Heeft(string capability);
}
