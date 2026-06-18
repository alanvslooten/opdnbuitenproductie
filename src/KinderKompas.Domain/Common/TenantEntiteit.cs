namespace KinderKompas.Domain.Common;

/// <summary>
/// Basis voor alle bedrijfs-entiteiten die bij een organisatie horen.
/// Voegt de tenant-sleutel toe aan <see cref="Entiteit"/>.
/// </summary>
public abstract class TenantEntiteit : Entiteit, ITenantEntiteit
{
    public Guid OrganisatieId { get; set; }
}
