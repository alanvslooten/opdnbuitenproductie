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

    /// <summary>
    /// Maximaal aantal kinderen dat PER DAG in deze groep aanwezig mag zijn (planning/BKR).
    /// Dit is bewust géén limiet op het totaal aantal thuisgroep-kinderen: die zijn over
    /// de week verspreid, dus een groep mag onbeperkt kinderen als thuisgroep hebben.
    /// </summary>
    public int MaxKinderen { get; set; } = 12;

    public Organisatie? Organisatie { get; set; }
    public ICollection<Kind> Kinderen { get; set; } = new List<Kind>();

    /// <summary>
    /// Of er bij het gegeven aantal aanwezige kinderen op een dag nog één bij kan zonder
    /// het dag-maximum te overschrijden. Wordt gebruikt voor de bezetting PER DAG (bijv.
    /// de voorstel-projectie), niet voor het totaal aantal thuisgroep-kinderen.
    /// </summary>
    public bool HeeftPlaatsVoorExtraKind(int aantalAanwezigOpDag) =>
        aantalAanwezigOpDag < MaxKinderen;
}
