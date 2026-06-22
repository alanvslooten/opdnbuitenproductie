using Microsoft.AspNetCore.Identity;

namespace KinderKompas.Infrastructure.Identity;

/// <summary>
/// Het login-account. Leeft in Infrastructure omdat <see cref="IdentityUser"/>
/// een framework-type is en het Domain dependency-vrij moet blijven. De
/// koppeling naar het domein loopt via <see cref="MedewerkerId"/> (en omgekeerd
/// via <c>Medewerker.IdentityUserId</c>). Het Groepsportaal is een gedeeld
/// account zonder persoon: dan blijft <see cref="MedewerkerId"/> leeg.
///
/// Gebruikt de standaard string-sleutel van Identity, passend bij het reeds
/// voorbereide <c>Medewerker.IdentityUserId</c> (ook string).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>De organisatie (tenant) waartoe dit account behoort.</summary>
    public Guid OrganisatieId { get; set; }

    /// <summary>De gekoppelde medewerker, of null voor een gedeeld portaal-account.</summary>
    public Guid? MedewerkerId { get; set; }

    /// <summary>
    /// De stamgroep waaraan dit account vast hangt — gezet voor een Groepsportaal-account
    /// (één tablet per groep, bv. Bengeltjes of Boefjes), zodat alle groepsportaal-data
    /// tot die groep beperkt blijft. Null voor persoonlijke/back-office accounts.
    /// </summary>
    public Guid? StamgroepId { get; set; }
}
