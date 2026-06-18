using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Koppelt binnen één organisatie een <see cref="Rol"/> aan een
/// <see cref="Capability"/>. Dit is de data-gedreven rechten-mapping: de
/// Beheerder kan per rol rechten aan- of uitzetten zonder code-wijziging
/// (sluit aan op de Instellingenpagina in fase 9). We seeden een verstandige
/// default. Tenant-scoped, zodat elke organisatie haar eigen rechtenmatrix heeft.
/// </summary>
public class RolCapability : TenantEntiteit
{
    public Rol Rol { get; set; }

    public Guid CapabilityId { get; set; }
    public Capability? Capability { get; set; }
}
