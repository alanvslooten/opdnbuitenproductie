using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KinderKompas.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KinderKompas.Infrastructure.Identity;

/// <inheritdoc />
public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opties;

    public TokenService(IOptions<JwtOptions> opties)
    {
        _opties = opties.Value;
    }

    public (string Token, DateTime VerlooptOpUtc) MaakAccessToken(
        ApplicationUser gebruiker, string? rol, IReadOnlyCollection<string> capabilities,
        string? stamgroepNaam = null, string? weergavenaam = null)
    {
        DateTime nu = DateTime.UtcNow;
        DateTime verlooptOp = nu.AddMinutes(_opties.AccessTokenMinuten);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, gebruiker.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, gebruiker.UserName ?? string.Empty),
            new(KinderKompasClaims.OrganisatieId, gebruiker.OrganisatieId.ToString()),
        };

        // Koppeling naar de medewerker (afwezig voor het gedeelde portaal-account).
        if (gebruiker.MedewerkerId is { } medewerkerId)
        {
            claims.Add(new Claim(KinderKompasClaims.MedewerkerId, medewerkerId.ToString()));
        }

        // Stamgroep-scope voor een Groepsportaal-account (één tablet per groep).
        if (gebruiker.StamgroepId is { } stamgroepId)
        {
            claims.Add(new Claim(KinderKompasClaims.StamgroepId, stamgroepId.ToString()));
        }
        if (!string.IsNullOrWhiteSpace(stamgroepNaam))
        {
            claims.Add(new Claim(KinderKompasClaims.StamgroepNaam, stamgroepNaam));
        }
        if (!string.IsNullOrWhiteSpace(weergavenaam))
        {
            claims.Add(new Claim(KinderKompasClaims.Weergavenaam, weergavenaam));
        }

        if (!string.IsNullOrWhiteSpace(rol))
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        foreach (string cap in capabilities)
        {
            claims.Add(new Claim(KinderKompasClaims.Capability, cap));
        }

        var sleutel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opties.Key));
        var credentials = new SigningCredentials(sleutel, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opties.Issuer,
            audience: _opties.Audience,
            claims: claims,
            notBefore: nu,
            expires: verlooptOp,
            signingCredentials: credentials);

        string serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return (serialized, verlooptOp);
    }

    public string GenereerRefreshTokenWaarde()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string waarde)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(waarde));
        return Convert.ToHexString(hash);
    }
}
