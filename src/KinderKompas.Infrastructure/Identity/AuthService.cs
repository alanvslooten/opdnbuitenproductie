using System.Text;
using System.Text.Encodings.Web;
using KinderKompas.Application.Auth;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KinderKompas.Infrastructure.Identity;

/// <inheritdoc />
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly KinderKompasDbContext _db;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _jwt;

    public AuthService(
        UserManager<ApplicationUser> users,
        KinderKompasDbContext db,
        ITokenService tokens,
        IOptions<JwtOptions> jwt)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
        _jwt = jwt.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        ApplicationUser gebruiker = await VindEnControleerWachtwoordAsync(request.Gebruikersnaam, request.Wachtwoord);

        // 2FA verplicht én ingericht → eerst een code vragen.
        if (await _users.GetTwoFactorEnabledAsync(gebruiker))
        {
            return AuthResponse.TweeFactorNodig();
        }

        return await BouwAuthResponseAsync(gebruiker, ct);
    }

    public async Task<AuthResponse> LoginTweeFactorAsync(TweeFactorLoginRequest request, CancellationToken ct)
    {
        ApplicationUser gebruiker = await VindEnControleerWachtwoordAsync(request.Gebruikersnaam, request.Wachtwoord);

        string code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        bool geldig = await _users.VerifyTwoFactorTokenAsync(
            gebruiker, _users.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!geldig)
        {
            throw new AuthException("Ongeldige of verlopen 2FA-code.");
        }

        return await BouwAuthResponseAsync(gebruiker, ct);
    }

    public async Task<AuthResponse> VernieuwAsync(VernieuwRequest request, CancellationToken ct)
    {
        string hash = _tokens.Hash(request.RefreshToken);

        RefreshToken? bestaand = await _db.RefreshTokens
            .Include(t => t.ApplicationUser)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (bestaand is null || !bestaand.IsActief || bestaand.ApplicationUser is null)
        {
            throw new AuthException("Ongeldige of verlopen refresh-token.");
        }

        // Rotatie: oude token intrekken en vervangen door een nieuwe.
        string nieuweWaarde = _tokens.GenereerRefreshTokenWaarde();
        bestaand.IngetrokkenOp = DateTime.UtcNow;
        bestaand.VervangenDoorTokenHash = _tokens.Hash(nieuweWaarde);

        return await BouwAuthResponseAsync(bestaand.ApplicationUser, ct, nieuweWaarde);
    }

    public async Task LogUitAsync(VernieuwRequest request, CancellationToken ct)
    {
        string hash = _tokens.Hash(request.RefreshToken);
        RefreshToken? bestaand = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (bestaand is { IngetrokkenOp: null })
        {
            bestaand.IngetrokkenOp = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<TweeFactorSetupResponse> TweeFactorSetupAsync(string userId, CancellationToken ct)
    {
        ApplicationUser gebruiker = await _users.FindByIdAsync(userId)
            ?? throw new AuthException("Gebruiker niet gevonden.");

        string? sleutel = await _users.GetAuthenticatorKeyAsync(gebruiker);
        if (string.IsNullOrEmpty(sleutel))
        {
            await _users.ResetAuthenticatorKeyAsync(gebruiker);
            sleutel = await _users.GetAuthenticatorKeyAsync(gebruiker);
        }

        return new TweeFactorSetupResponse(sleutel!, MaakAuthenticatorUri(gebruiker.Email ?? gebruiker.UserName!, sleutel!));
    }

    public async Task<TweeFactorGeactiveerdResponse> TweeFactorActiverenAsync(
        string userId, TweeFactorActiverenRequest request, CancellationToken ct)
    {
        ApplicationUser gebruiker = await _users.FindByIdAsync(userId)
            ?? throw new AuthException("Gebruiker niet gevonden.");

        string code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        bool geldig = await _users.VerifyTwoFactorTokenAsync(
            gebruiker, _users.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!geldig)
        {
            throw new AuthException("De ingevoerde code klopt niet. Controleer de tijd op je apparaat.");
        }

        await _users.SetTwoFactorEnabledAsync(gebruiker, true);
        IEnumerable<string> herstelcodes =
            await _users.GenerateNewTwoFactorRecoveryCodesAsync(gebruiker, 10) ?? Enumerable.Empty<string>();

        return new TweeFactorGeactiveerdResponse(herstelcodes.ToList());
    }

    // ---- privé helpers -------------------------------------------------------

    private async Task<ApplicationUser> VindEnControleerWachtwoordAsync(string gebruikersnaam, string wachtwoord)
    {
        ApplicationUser? gebruiker = await _users.FindByNameAsync(gebruikersnaam)
            ?? await _users.FindByEmailAsync(gebruikersnaam);

        // Bewust dezelfde foutmelding voor onbekende gebruiker en fout wachtwoord
        // (geen account-enumeratie). Lockout respecteren.
        if (gebruiker is null || await _users.IsLockedOutAsync(gebruiker))
        {
            throw new AuthException("Onjuiste gebruikersnaam of wachtwoord.");
        }

        if (!await _users.CheckPasswordAsync(gebruiker, wachtwoord))
        {
            await _users.AccessFailedAsync(gebruiker);
            throw new AuthException("Onjuiste gebruikersnaam of wachtwoord.");
        }

        await _users.ResetAccessFailedCountAsync(gebruiker);
        return gebruiker;
    }

    private async Task<AuthResponse> BouwAuthResponseAsync(
        ApplicationUser gebruiker, CancellationToken ct, string? refreshWaarde = null)
    {
        (string? rolNaam, IReadOnlyList<string> capabilities) = await HaalRolEnCapabilitiesAsync(gebruiker, ct);

        // Groepsportaal-account: de groepsnaam erbij voor de scope/weergave (zijbalk).
        string? stamgroepNaam = gebruiker.StamgroepId is { } sid
            ? await _db.Stamgroepen.IgnoreQueryFilters().AsNoTracking()
                .Where(s => s.Id == sid).Select(s => s.Naam).FirstOrDefaultAsync(ct)
            : null;

        // Weergavenaam: de volledige naam van de gekoppelde medewerker (de zijbalk toont
        // die i.p.v. de kale inlognaam). Afwezig voor het gedeelde portaal-account.
        string? weergavenaam = gebruiker.MedewerkerId is { } mid
            ? await _db.Medewerkers.IgnoreQueryFilters().AsNoTracking()
                .Where(m => m.Id == mid).Select(m => m.Voornaam + " " + m.Achternaam).FirstOrDefaultAsync(ct)
            : null;

        (string accessToken, DateTime verlooptOp) =
            _tokens.MaakAccessToken(gebruiker, rolNaam, capabilities, stamgroepNaam, weergavenaam);

        refreshWaarde ??= _tokens.GenereerRefreshTokenWaarde();
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = _tokens.Hash(refreshWaarde),
            ApplicationUserId = gebruiker.Id,
            OrganisatieId = gebruiker.OrganisatieId,
            AangemaaktOp = DateTime.UtcNow,
            VerlooptOp = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDagen),
        });
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            false, accessToken, refreshWaarde, verlooptOp, rolNaam, capabilities, stamgroepNaam, weergavenaam);
    }

    /// <summary>
    /// Bepaalt de functionele rol van de gebruiker en de bijbehorende capabilities
    /// uit de data-gedreven rechten-mapping. IgnoreQueryFilters omdat login plaatsvindt
    /// vóórdat de tenant-context (uit de claim) bestaat; we filteren expliciet op de
    /// organisatie van de gebruiker.
    /// </summary>
    private async Task<(string? RolNaam, IReadOnlyList<string> Capabilities)> HaalRolEnCapabilitiesAsync(
        ApplicationUser gebruiker, CancellationToken ct)
    {
        IList<string> rollen = await _users.GetRolesAsync(gebruiker);
        string? rolNaam = rollen.FirstOrDefault();

        if (rolNaam is null || !Enum.TryParse(rolNaam, out Rol rol))
        {
            return (rolNaam, Array.Empty<string>());
        }

        List<string> capabilities = await _db.RolCapabilities
            .IgnoreQueryFilters()
            .Where(rc => rc.OrganisatieId == gebruiker.OrganisatieId && rc.Rol == rol)
            .Select(rc => rc.Capability!.Sleutel)
            .ToListAsync(ct);

        return (rolNaam, capabilities);
    }

    private string MaakAuthenticatorUri(string account, string sleutel)
    {
        const string digits = "6";
        return
            $"otpauth://totp/{UrlEncoder.Default.Encode(_jwt.Issuer)}:{UrlEncoder.Default.Encode(account)}" +
            $"?secret={sleutel}&issuer={UrlEncoder.Default.Encode(_jwt.Issuer)}&digits={digits}";
    }
}
