using System.Security.Claims;

namespace KinderKompas.Infrastructure.Identity;

/// <summary>
/// Maakt access-tokens (JWT) en refresh-token-waarden. Bevat geen
/// database-toegang: het bewaren/rouleren van refresh-tokens doet de
/// <see cref="IAuthService"/>.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Bouwt een ondertekend JWT met de standaard- en KinderKompas-claims.
    /// </summary>
    (string Token, DateTime VerlooptOpUtc) MaakAccessToken(
        ApplicationUser gebruiker, string? rol, IReadOnlyCollection<string> capabilities);

    /// <summary>Genereert een nieuwe, cryptografisch sterke refresh-token-waarde.</summary>
    string GenereerRefreshTokenWaarde();

    /// <summary>SHA-256-hash (hex) van een token-waarde, voor opslag/vergelijking.</summary>
    string Hash(string waarde);
}
