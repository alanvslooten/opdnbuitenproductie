namespace KinderKompas.Domain.Meldingen;

/// <summary>
/// Basis voor de domein-events die het actiecentrum (fase 9) voeden. Een module
/// publiceert zo'n gebeurtenis op een betekenisvol moment (verlof aangevraagd, kind
/// geplaatst, …); de <see cref="Services.MeldingFabriek"/> vertaalt 'm naar een
/// <see cref="Entiteiten.Melding"/>. Events dragen alleen de gegevens die nodig zijn
/// om de melding-tekst en de deep-link op te bouwen — geen entiteit-referenties.
/// </summary>
public abstract record MeldingGebeurtenis;

/// <summary>Een nieuwe wachtlijstaanmelding is binnengekomen.</summary>
public sealed record NieuweWachtlijstaanmelding(Guid InschrijvingId, string KindNaam) : MeldingGebeurtenis;

/// <summary>Een medewerker heeft verlof aangevraagd; de planner moet het beoordelen.</summary>
public sealed record VerlofAangevraagd(
    Guid AanvraagId,
    string MedewerkerNaam,
    DateOnly Begindatum,
    DateOnly Einddatum,
    decimal AantalUren) : MeldingGebeurtenis;

/// <summary>Een medewerker is ziek gemeld; controleer of een invaller nodig is.</summary>
public sealed record Ziekgemeld(
    Guid ZiekmeldingId,
    string MedewerkerNaam,
    DateOnly Begindatum) : MeldingGebeurtenis;

/// <summary>Een plaatsingsvoorstel is geaccepteerd; er moet een contract in Portabase komen.</summary>
public sealed record PlaatsingGeaccepteerd(
    Guid InschrijvingId,
    string KindNaam,
    DateOnly Startdatum,
    bool VolledigGeplaatst) : MeldingGebeurtenis;

/// <summary>De BKR-ratio dreigt op een dag te worden overschreden (uit de rekenkern).</summary>
public sealed record BkrOverschrijdingGesignaleerd(
    Guid StamgroepId,
    string StamgroepNaam,
    DateOnly Datum,
    int AantalKinderen,
    int VereistePmers) : MeldingGebeurtenis;

/// <summary>Een observatiemoment van een kind vraagt aandacht (komt eraan / is verstreken).</summary>
public sealed record Observatieherinnering(
    Guid KindId,
    string KindNaam,
    int MijlpaalMaanden) : MeldingGebeurtenis;
