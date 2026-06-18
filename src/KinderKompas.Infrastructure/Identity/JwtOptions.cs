namespace KinderKompas.Infrastructure.Identity;

/// <summary>
/// Configuratie van de JWT-uitgifte en -validatie. Gebonden aan de sectie "Jwt"
/// in de configuratie. De <see cref="Key"/> is een SECRET en hoort lokaal in
/// user-secrets en in productie in Key Vault — nooit in appsettings in broncode.
/// </summary>
public sealed class JwtOptions
{
    public const string Sectie = "Jwt";

    public string Issuer { get; set; } = "KinderKompas";
    public string Audience { get; set; } = "KinderKompas.Client";

    /// <summary>Symmetrische ondertekeningssleutel (minimaal 32 tekens voor HS256).</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinuten { get; set; } = 15;
    public int RefreshTokenDagen { get; set; } = 7;
}
