namespace KinderKompas.Domain.Enums;

/// <summary>
/// Het opvangcontract bepaalt over hoeveel weken per jaar de afgenomen dagen
/// gespreid zijn. Relevant voor planning en (later) facturatie buiten scope.
/// </summary>
public enum Contracttype
{
    /// <summary>Opvang gedurende 49 weken per jaar (inclusief schoolvakanties).</summary>
    Weken49 = 49,

    /// <summary>Opvang gedurende 40 weken per jaar (alleen schoolweken).</summary>
    Weken40 = 40
}
