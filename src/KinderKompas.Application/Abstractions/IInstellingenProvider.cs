using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Abstractions;

/// <summary>
/// Levert de instellingen van de huidige organisatie (tenant). Modules vragen hun
/// instelbaar gedrag ALTIJD via deze provider op, zodat ze niet zelf de DbContext of
/// de tenant kennen. De implementatie (Infrastructure) laadt de — geseede — rij voor
/// de huidige organisatie en maakt 'm defensief aan als die onverhoopt ontbreekt.
/// </summary>
public interface IInstellingenProvider
{
    Task<OrganisatieInstellingen> HuidigeAsync(CancellationToken ct = default);
}
