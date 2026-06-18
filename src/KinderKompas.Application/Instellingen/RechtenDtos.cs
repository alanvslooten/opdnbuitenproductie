using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Instellingen;

/// <summary>Eén capability uit de catalogus: de sleutel en een leesbare omschrijving.</summary>
public sealed record CapabilityInfoDto(string Sleutel, string Omschrijving);

/// <summary>De toegekende capability-sleutels van één rol.</summary>
public sealed record RolRechtenDto(Rol Rol, IReadOnlyList<string> Capabilities);

/// <summary>
/// De volledige rechten-matrix (fase 9c): de capability-catalogus (voor de kolomkoppen
/// in de UI) plus per rol de toegekende rechten.
/// </summary>
public sealed record RechtenMatrixDto(
    IReadOnlyList<CapabilityInfoDto> Capabilities,
    IReadOnlyList<RolRechtenDto> Rollen);

/// <summary>Invoer om de rechten van één rol volledig te vervangen (lijst van capability-sleutels).</summary>
public sealed record RolRechtenInvoer(IReadOnlyList<string> Capabilities);
