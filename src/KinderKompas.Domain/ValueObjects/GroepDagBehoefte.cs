namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// De personeelsbehoefte van één stamgroep op één dag: hoeveel pm'ers er wettelijk
/// nodig zijn (de uitkomst van de <see cref="Services.BkrCalculator"/>). Dit is de
/// LEIDENDE input voor de auto-rooster-generator: niet "wie is beschikbaar", maar
/// "hoeveel zijn er nodig" bepaalt of er wordt bijgeplaatst.
/// </summary>
public readonly record struct GroepDagBehoefte(
    Guid StamgroepId,
    DateOnly Datum,
    int NodigPmers);
