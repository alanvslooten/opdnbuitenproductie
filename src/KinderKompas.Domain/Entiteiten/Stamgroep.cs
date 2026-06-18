using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een fysieke stamgroep (bijv. "Bengeltjes", "Boefjes"): de vaste groep en
/// ruimte waarin een kind wordt opgevangen. Let op: dit is iets anders dan de
/// wettelijke leeftijdscategorie (zie <c>Leeftijdscategorie</c>).
/// </summary>
public class Stamgroep : TenantEntiteit
{
    public required string Naam { get; set; }

    /// <summary>Maximaal aantal kindplaatsen in deze groep.</summary>
    public int MaxKinderen { get; set; } = 12;

    public Organisatie? Organisatie { get; set; }
    public ICollection<Kind> Kinderen { get; set; } = new List<Kind>();

    /// <summary>
    /// Of er nog plaats is om bij het gegeven huidige aantal kinderen er één bij te
    /// plaatsen zonder het groepsmaximum te overschrijden. Voorkomt de "13e plaatsing"
    /// in een groep van 12.
    /// </summary>
    public bool HeeftPlaatsVoorExtraKind(int huidigAantalKinderen) =>
        huidigAantalKinderen < MaxKinderen;
}
