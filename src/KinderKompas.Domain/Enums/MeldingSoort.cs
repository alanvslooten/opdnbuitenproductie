namespace KinderKompas.Domain.Enums;

/// <summary>
/// Het soort melding/to-do, één per trigger-type dat het app-brede actiecentrum
/// (fase 9) voedt. De soort bepaalt de iconografie/filtering in de UI en — via de
/// <see cref="Services.MeldingFabriek"/> — of het een informatieve melding of een
/// af te vinken to-do is.
/// </summary>
public enum MeldingSoort
{
    /// <summary>BKR-ratio dreigt te worden overschreden (uit de rekenkern, fase 2).</summary>
    BkrWaarschuwing = 0,

    /// <summary>Een observatiemoment van een kind komt eraan of is verstreken (fase 7).</summary>
    Observatieherinnering = 1,

    /// <summary>Een medewerker heeft verlof aangevraagd dat beoordeeld moet worden (fase 5).</summary>
    Verlofaanvraag = 2,

    /// <summary>Een medewerker is ziek gemeld; controleer of een invaller nodig is (fase 5).</summary>
    Ziekmelding = 3,

    /// <summary>Een plaatsingsvoorstel is geaccepteerd; contract opmaken in Portabase (fase 6).</summary>
    VoorstelGeaccepteerd = 4,

    /// <summary>Een nieuwe wachtlijstaanmelding is binnengekomen (fase 6).</summary>
    NieuweWachtlijstaanmelding = 5,
}
