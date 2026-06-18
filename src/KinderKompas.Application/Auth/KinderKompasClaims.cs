namespace KinderKompas.Application.Auth;

/// <summary>
/// De custom claim-types die in het JWT worden opgenomen, naast de standaard
/// claims (sub, naam, rol). Eén plek zodat token-generatie en token-lezing
/// dezelfde sleutels gebruiken.
/// </summary>
public static class KinderKompasClaims
{
    /// <summary>De organisatie (tenant) waartoe de gebruiker behoort.</summary>
    public const string OrganisatieId = "organisatieId";

    /// <summary>
    /// De medewerker gekoppeld aan het account. Afwezig voor een gedeeld portaal-account.
    /// Stuurt o.a. de zichtbaarheid van observaties (mentor ziet eigen kinderen).
    /// </summary>
    public const string MedewerkerId = "medewerkerId";

    /// <summary>Eén claim per capability (recht) van de gebruiker.</summary>
    public const string Capability = "cap";
}
