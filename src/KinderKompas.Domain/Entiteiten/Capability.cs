using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Referentierecord voor één capability (fijnmazig recht) dat het systeem kent.
/// Dit is systeemkennis, gedeeld over alle organisaties, en valt daarom bewust
/// BUITEN de tenant-queryfilter (erft van <see cref="Entiteit"/>, niet van
/// <see cref="TenantEntiteit"/>). De toewijzing aan rollen is wél per
/// organisatie instelbaar; zie <see cref="RolCapability"/>.
/// </summary>
public class Capability : Entiteit
{
    /// <summary>Unieke sleutel, gelijk aan de constante in <c>Capabilities</c>.</summary>
    public required string Sleutel { get; set; }

    public required string Omschrijving { get; set; }

    public ICollection<RolCapability> RolCapabilities { get; set; } = new List<RolCapability>();
}
