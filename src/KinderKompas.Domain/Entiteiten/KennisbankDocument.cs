using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén document in de interne kennisbank (v3): read-only naslag voor medewerkers —
/// protocollen, het pedagogisch beleidsplan, kledingvoorschriften e.d. Voor iedereen
/// gelijk en ook thuis inzichtelijk. GEEN taak-/instructiesysteem: puur naslag.
/// De beheerder onderhoudt de documenten; medewerkers lezen ze alleen.
/// </summary>
public class KennisbankDocument : TenantEntiteit
{
    /// <summary>Titel van het document (bijv. "Pedagogisch beleidsplan").</summary>
    public required string Titel { get; set; }

    /// <summary>Optionele rubriek om documenten te groeperen (bijv. "Protocollen").</summary>
    public string? Categorie { get; set; }

    /// <summary>De inhoud als platte tekst/markdown. Read-only voor medewerkers.</summary>
    public required string Inhoud { get; set; }
}
