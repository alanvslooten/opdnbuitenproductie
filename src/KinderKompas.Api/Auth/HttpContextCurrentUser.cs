using System.Security.Claims;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Auth;

namespace KinderKompas.Api.Auth;

/// <summary>
/// Leest de ingelogde gebruiker uit de claims van de huidige HTTP-request.
/// Hiermee kan business-logica rechten opvragen zonder de HttpContext te kennen.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsGeauthenticeerd => Principal?.Identity?.IsAuthenticated ?? false;

    public string? UserId => Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public Guid? OrganisatieId =>
        Guid.TryParse(Principal?.FindFirst(KinderKompasClaims.OrganisatieId)?.Value, out Guid id)
            ? id
            : null;

    public Guid? MedewerkerId =>
        Guid.TryParse(Principal?.FindFirst(KinderKompasClaims.MedewerkerId)?.Value, out Guid id)
            ? id
            : null;

    public Guid? StamgroepId =>
        Guid.TryParse(Principal?.FindFirst(KinderKompasClaims.StamgroepId)?.Value, out Guid id)
            ? id
            : null;

    public IReadOnlySet<string> Capabilities =>
        Principal?.FindAll(KinderKompasClaims.Capability).Select(c => c.Value).ToHashSet()
        ?? new HashSet<string>();

    public bool Heeft(string capability) => Capabilities.Contains(capability);
}
