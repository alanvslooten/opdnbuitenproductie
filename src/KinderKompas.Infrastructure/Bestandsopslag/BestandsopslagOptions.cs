namespace KinderKompas.Infrastructure.Bestandsopslag;

/// <summary>
/// Instellingen voor de lokale bestandsopslag. In productie (Azure) wordt dit
/// vervangen door een Blob Storage-implementatie; de aanroepers (via
/// <c>IBestandsopslag</c>) merken daar niets van.
/// </summary>
public sealed class BestandsopslagOptions
{
    public const string Sectie = "Bestandsopslag";

    /// <summary>
    /// De hoofdmap waar bestanden worden opgeslagen. Leeg = een standaardmap onder
    /// de applicatie-basismap (<c>App_Data/bestanden</c>).
    /// </summary>
    public string? Root { get; set; }
}
