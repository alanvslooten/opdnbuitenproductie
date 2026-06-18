namespace KinderKompas.Domain.Enums;

/// <summary>
/// De beoordelingsstatus van een verlofaanvraag. Stuurt zowel het archief
/// (statusfilters) als de kleurcodering in het rooster aan: een openstaande
/// aanvraag kleurt oranje, een goedgekeurde groen; een afgekeurde verdwijnt.
/// </summary>
public enum VerlofStatus
{
    /// <summary>Aangevraagd, nog niet beoordeeld (oranje in het rooster).</summary>
    Openstaand = 0,

    /// <summary>Goedgekeurd (groen in het rooster; wordt ALTIJD gerespecteerd door het auto-rooster).</summary>
    Goedgekeurd = 1,

    /// <summary>Afgekeurd (verdwijnt uit het rooster).</summary>
    Afgekeurd = 2
}
