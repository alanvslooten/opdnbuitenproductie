using KinderKompas.Application.Abstractions;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Infrastructure.Instellingen;

/// <summary>
/// Laadt de instellingen-rij van de huidige organisatie via de tenant-gefilterde
/// DbContext (er is er per organisatie precies één). Ontbreekt de rij — bijv. een
/// organisatie van vóór deze migratie — dan wordt er één met de code-defaults gemaakt,
/// zodat aanroepers nooit met null hoeven te rekenen.
/// </summary>
public sealed class InstellingenProvider : IInstellingenProvider
{
    private readonly KinderKompasDbContext _db;

    public InstellingenProvider(KinderKompasDbContext db)
    {
        _db = db;
    }

    public async Task<OrganisatieInstellingen> HuidigeAsync(CancellationToken ct = default)
    {
        OrganisatieInstellingen? instellingen =
            await _db.OrganisatieInstellingen.FirstOrDefaultAsync(ct);

        if (instellingen is null)
        {
            instellingen = new OrganisatieInstellingen();
            _db.OrganisatieInstellingen.Add(instellingen);
            await _db.SaveChangesAsync(ct);
        }

        return instellingen;
    }
}
