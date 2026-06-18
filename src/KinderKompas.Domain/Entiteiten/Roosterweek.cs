using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Het werkrooster van één kalenderweek. Houdt de concept/verstuurd-status vast en
/// groepeert de diensten van die week. Een auto-rooster wordt als <see cref="RoosterStatus.Concept"/>
/// opgeslagen; pas na een expliciete "versturen"-actie wordt het
/// <see cref="RoosterStatus.Verstuurd"/> en zichtbaar voor de medewerkers.
/// </summary>
public class Roosterweek : TenantEntiteit
{
    /// <summary>De maandag van de week (per organisatie uniek).</summary>
    public DateOnly WeekBegin { get; set; }

    public RoosterStatus Status { get; set; } = RoosterStatus.Concept;

    /// <summary>Tijdstip (UTC) waarop het rooster definitief is verstuurd. Null zolang het concept is.</summary>
    public DateTime? VerstuurdOp { get; set; }

    public ICollection<Roosterdienst> Diensten { get; set; } = new List<Roosterdienst>();

    public bool IsVerstuurd => Status == RoosterStatus.Verstuurd;
}
