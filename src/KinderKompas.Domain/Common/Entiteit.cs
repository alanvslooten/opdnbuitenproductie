namespace KinderKompas.Domain.Common;

/// <summary>
/// Basis voor elke entiteit in het domein. Levert de identiteit (Guid) en
/// audit-velden. De waarden van de audit-velden worden centraal gezet in de
/// SaveChanges-override van de DbContext, niet handmatig in business-logica.
/// </summary>
public abstract class Entiteit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tijdstip (UTC) waarop de entiteit is aangemaakt.</summary>
    public DateTime AangemaaktOp { get; set; }

    /// <summary>Tijdstip (UTC) van de laatste wijziging.</summary>
    public DateTime GewijzigdOp { get; set; }
}
