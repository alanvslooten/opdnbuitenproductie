using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// Eén regel uit het auto-rooster-voorstel: een medewerker die op een dag in een
/// stamgroep wordt voorgesteld, met de reden (vast of bijgeplaatst). Het voorstel is
/// puur advies; de planner beslist en stuurt het daarna pas expliciet door.
/// </summary>
public readonly record struct RoosterVoorstelRegel(
    Guid StamgroepId,
    DateOnly Datum,
    Guid MedewerkerId,
    RoosterBron Bron);
