using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén voorgestelde opvangdag binnen een <see cref="Voorstel"/>: de weekdag en de
/// concrete startdatum die de planner ("Gail") voor díe dag voorstelt. Zo kan een
/// voorstel per dag een andere ingangsdatum hebben (de ene plek komt eerder vrij
/// dan de andere).
/// </summary>
public class VoorstelDag : TenantEntiteit
{
    public Guid VoorstelId { get; set; }
    public Voorstel? Voorstel { get; set; }

    /// <summary>De weekdag (één enkele vlag, geen combinatie).</summary>
    public Weekdag Weekdag { get; set; }

    /// <summary>De voorgestelde concrete startdatum voor deze dag.</summary>
    public DateOnly VoorgesteldeDatum { get; set; }
}
