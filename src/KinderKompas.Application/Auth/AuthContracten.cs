namespace KinderKompas.Application.Auth;

/// <summary>De contracten (request/response-DTO's) van de authenticatie-endpoints.</summary>

public sealed record LoginRequest(string Gebruikersnaam, string Wachtwoord);

public sealed record TweeFactorLoginRequest(string Gebruikersnaam, string Wachtwoord, string Code);

public sealed record VernieuwRequest(string RefreshToken);

/// <summary>
/// Het resultaat van een geslaagde login of token-vernieuwing. <see cref="VereistTweeFactor"/>
/// is true wanneer wachtwoord klopt maar er nog een 2FA-code nodig is; dan zijn de
/// token-velden leeg en moet de client <c>/auth/login-2fa</c> aanroepen.
/// </summary>
public sealed record AuthResponse(
    bool VereistTweeFactor,
    string? AccessToken,
    string? RefreshToken,
    DateTime? VerlooptOpUtc,
    string? Rol,
    IReadOnlyList<string> Capabilities,
    string? StamgroepNaam = null,
    string? Weergavenaam = null)
{
    public static AuthResponse TweeFactorNodig() =>
        new(true, null, null, null, null, Array.Empty<string>());
}

/// <summary>Gegevens om een authenticator-app te koppelen (2FA-inrichting).</summary>
public sealed record TweeFactorSetupResponse(string GedeeldeSleutel, string AuthenticatorUri);

public sealed record TweeFactorActiverenRequest(string Code);

/// <summary>Herstelcodes die de gebruiker eenmalig te zien krijgt na activeren van 2FA.</summary>
public sealed record TweeFactorGeactiveerdResponse(IReadOnlyList<string> Herstelcodes);
