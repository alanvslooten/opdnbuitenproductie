using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Auth;

namespace KinderKompas.Api.Auth;

/// <summary>
/// Tenant-provider die de organisatie uit de JWT-claim van de ingelogde
/// gebruiker leest (vervangt de vaste provider uit fase 1/2). Is er geen geldige
/// claim, dan is er geen tenant-context: dat hoort alleen te gebeuren op anonieme
/// endpoints (login), die de tenant-queryfilter niet gebruiken.
/// </summary>
public sealed class ClaimsTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _accessor;

    public ClaimsTenantProvider(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid CurrentOrganisatieId
    {
        get
        {
            string? waarde = _accessor.HttpContext?.User.FindFirst(KinderKompasClaims.OrganisatieId)?.Value;
            if (Guid.TryParse(waarde, out Guid organisatieId))
            {
                return organisatieId;
            }

            throw new InvalidOperationException(
                "Geen organisatie-claim aanwezig; deze operatie vereist een geauthenticeerde gebruiker.");
        }
    }
}
