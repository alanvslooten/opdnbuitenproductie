using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een rondleiding bij een <see cref="Contact"/>: onderdeel van de contacthistorie.
/// Houdt datum, status en een optionele notitie bij.
/// </summary>
public class Rondleiding : TenantEntiteit
{
    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    public DateOnly Datum { get; set; }

    public RondleidingStatus Status { get; set; } = RondleidingStatus.Gepland;

    public string? Notitie { get; set; }

    public Organisatie? Organisatie { get; set; }
}
