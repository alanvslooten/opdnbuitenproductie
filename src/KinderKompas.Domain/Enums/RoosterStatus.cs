namespace KinderKompas.Domain.Enums;

/// <summary>
/// De status van een roosterweek. Kernprincipe van fase 5: een auto-rooster is
/// altijd eerst een VOORSTEL (concept) dat de planner kan aanpassen; medewerkers
/// zien het rooster pas nadat het expliciet is VERSTUURD.
/// </summary>
public enum RoosterStatus
{
    /// <summary>Voorstel/werkversie. Alleen zichtbaar voor de planner.</summary>
    Concept = 0,

    /// <summary>Definitief verstuurd. Zichtbaar voor de medewerkers.</summary>
    Verstuurd = 1
}
