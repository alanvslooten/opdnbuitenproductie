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

    /// <summary>
    /// De medewerkers aan wie dit document is toegewezen. <b>Leeg = voor iedereen</b>
    /// zichtbaar; met één of meer medewerkers zien alléén die medewerkers (en de
    /// beheerder) het document. Opgeslagen als primitieve collectie (JSON/array).
    /// </summary>
    public List<Guid> ToegewezenMedewerkerIds { get; set; } = new();

    /// <summary>Of dit document aan specifieke medewerkers is toegewezen (i.p.v. voor iedereen).</summary>
    public bool IsGericht => ToegewezenMedewerkerIds.Count > 0;

    /// <summary>
    /// Of het document zichtbaar is voor de gegeven medewerker: altijd als het voor
    /// iedereen is, anders alleen als de medewerker in de toewijzing staat.
    /// </summary>
    public bool ZichtbaarVoor(Guid? medewerkerId) =>
        ToegewezenMedewerkerIds.Count == 0 ||
        (medewerkerId is { } id && ToegewezenMedewerkerIds.Contains(id));
}
