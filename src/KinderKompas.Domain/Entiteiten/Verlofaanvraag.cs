using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een verlofaanvraag van een medewerker over een aaneengesloten datumbereik, in
/// uren afgeboekt van een saldo (<see cref="VerlofCategorie"/>). De aanvraag
/// doorloopt de statussen openstaand → goedgekeurd/afgekeurd; goedgekeurd verlof
/// wordt door het auto-rooster ALTIJD gerespecteerd (er wordt nooit iemand op
/// goedgekeurd verlof ingepland).
/// </summary>
public class Verlofaanvraag : TenantEntiteit
{
    public Guid MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }

    /// <summary>Eerste verlofdag (inclusief).</summary>
    public DateOnly Begindatum { get; set; }

    /// <summary>Laatste verlofdag (inclusief).</summary>
    public DateOnly Einddatum { get; set; }

    /// <summary>Aantal af te boeken uren voor de gehele aanvraag.</summary>
    public decimal AantalUren { get; set; }

    public VerlofCategorie Categorie { get; set; }

    public VerlofStatus Status { get; set; } = VerlofStatus.Openstaand;

    /// <summary>Optionele toelichting van de aanvrager.</summary>
    public string? Reden { get; set; }

    /// <summary>Optionele notitie van de beoordelaar (bijv. reden van afkeuring).</summary>
    public string? BeoordelingsNotitie { get; set; }

    /// <summary>Tijdstip (UTC) van beoordeling. Null zolang de aanvraag openstaat.</summary>
    public DateTime? BeoordeeldOp { get; set; }

    /// <summary>Of het verlofbereik de gegeven datum omvat.</summary>
    public bool OmvatDatum(DateOnly datum) => Begindatum <= datum && datum <= Einddatum;

    /// <summary>Of deze aanvraag goedgekeurd is (en dus het rooster blokkeert).</summary>
    public bool IsGoedgekeurd => Status == VerlofStatus.Goedgekeurd;
}
