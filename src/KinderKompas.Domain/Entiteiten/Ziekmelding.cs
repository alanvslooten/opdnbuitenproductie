using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een ziekmelding van een medewerker. Kan open einde hebben (nog niet hersteld):
/// dan is <see cref="Einddatum"/> null en geldt de ziekte vanaf de begindatum
/// onbepaald door. Een zieke medewerker wordt door het auto-rooster niet ingepland
/// (rood in het rooster) en kan diens plek door een beschikbare kracht laten vullen.
/// </summary>
public class Ziekmelding : TenantEntiteit
{
    public Guid MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }

    /// <summary>Eerste ziektedag (inclusief).</summary>
    public DateOnly Begindatum { get; set; }

    /// <summary>Laatste ziektedag (inclusief), of null zolang de medewerker niet hersteld is.</summary>
    public DateOnly? Einddatum { get; set; }

    /// <summary>Of de ziekmelding de gegeven datum omvat (open einde telt door).</summary>
    public bool OmvatDatum(DateOnly datum) =>
        Begindatum <= datum && (Einddatum is null || datum <= Einddatum);
}
