namespace KinderKompas.Domain.Enums;

/// <summary>
/// Soort dienst binnen het rooster. Op locatie wordt onderscheid gemaakt tussen de
/// vroege dienst (openen) en de late dienst (sluiten); een reguliere dienst is geen
/// van beide.
/// </summary>
public enum Dienstsoort
{
    /// <summary>Reguliere dienst (geen specifieke openen/sluiten-rol).</summary>
    Regulier = 0,

    /// <summary>Vroege dienst — verantwoordelijk voor het openen.</summary>
    Vroege = 1,

    /// <summary>Late dienst — verantwoordelijk voor het sluiten.</summary>
    Late = 2
}
