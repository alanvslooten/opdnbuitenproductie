using System.Security.Claims;
using KinderKompas.Application.Auth;
using KinderKompas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Authenticatie-endpoints: wachtwoord-login (met 2FA-stap), token-vernieuwing,
/// uitloggen en het inrichten/activeren van 2FA.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Wachtwoord-login. Geeft tokens terug, of <c>VereistTweeFactor</c> bij 2FA.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
        => await Voer(() => _auth.LoginAsync(request, ct));

    /// <summary>Login afronden met de 2FA-code uit de authenticator-app.</summary>
    [AllowAnonymous]
    [HttpPost("login-2fa")]
    public async Task<ActionResult<AuthResponse>> LoginTweeFactor(TweeFactorLoginRequest request, CancellationToken ct)
        => await Voer(() => _auth.LoginTweeFactorAsync(request, ct));

    /// <summary>Een verlopen access-token vernieuwen met een geldig refresh-token (met rotatie).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(VernieuwRequest request, CancellationToken ct)
        => await Voer(() => _auth.VernieuwAsync(request, ct));

    /// <summary>Het refresh-token intrekken (uitloggen).</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(VernieuwRequest request, CancellationToken ct)
    {
        await _auth.LogUitAsync(request, ct);
        return NoContent();
    }

    /// <summary>Start 2FA-inrichting: geeft de gedeelde sleutel en authenticator-URI.</summary>
    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TweeFactorSetupResponse>> TweeFactorSetup(CancellationToken ct)
        => await Voer(() => _auth.TweeFactorSetupAsync(HuidigeUserId(), ct));

    /// <summary>Activeer 2FA door een geldige code te bevestigen; geeft herstelcodes terug.</summary>
    [Authorize]
    [HttpPost("2fa/activeren")]
    public async Task<ActionResult<TweeFactorGeactiveerdResponse>> TweeFactorActiveren(
        TweeFactorActiverenRequest request, CancellationToken ct)
        => await Voer(() => _auth.TweeFactorActiverenAsync(HuidigeUserId(), request, ct));

    /// <summary>Eenvoudige zelf-inspectie: wie ben ik en welke rechten heb ik?</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult WieBenIk() => Ok(new
    {
        userId = HuidigeUserId(),
        gebruikersnaam = User.Identity?.Name,
        organisatieId = User.FindFirst(KinderKompasClaims.OrganisatieId)?.Value,
        rollen = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
        capabilities = User.FindAll(KinderKompasClaims.Capability).Select(c => c.Value),
    });

    private string HuidigeUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthException("Geen geldige gebruiker in de token.");

    private async Task<ActionResult<T>> Voer<T>(Func<Task<T>> actie)
    {
        try
        {
            return Ok(await actie());
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { fout = ex.Message });
        }
    }
}
