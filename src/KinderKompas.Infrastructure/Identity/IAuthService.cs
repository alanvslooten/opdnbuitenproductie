using KinderKompas.Application.Auth;

namespace KinderKompas.Infrastructure.Identity;

/// <summary>
/// De authenticatie-orkestratie: login (met en zonder 2FA), token-vernieuwing,
/// uitloggen en het inrichten/activeren van 2FA. Gooit <see cref="AuthException"/>
/// bij verkeerde inloggegevens of een ongeldige code; de controller vertaalt dat
/// naar 401/400.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> LoginTweeFactorAsync(TweeFactorLoginRequest request, CancellationToken ct);
    Task<AuthResponse> VernieuwAsync(VernieuwRequest request, CancellationToken ct);
    Task LogUitAsync(VernieuwRequest request, CancellationToken ct);

    Task<TweeFactorSetupResponse> TweeFactorSetupAsync(string userId, CancellationToken ct);
    Task<TweeFactorGeactiveerdResponse> TweeFactorActiverenAsync(string userId, TweeFactorActiverenRequest request, CancellationToken ct);
}

/// <summary>Authenticatie-fout (verkeerde inloggegevens, ongeldige/verlopen token of code).</summary>
public sealed class AuthException(string boodschap) : Exception(boodschap);
