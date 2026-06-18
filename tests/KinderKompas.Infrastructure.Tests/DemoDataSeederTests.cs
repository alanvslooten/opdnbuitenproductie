using KinderKompas.Application.Abstractions;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KinderKompas.Infrastructure.Tests;

/// <summary>
/// Smoke-tests voor de <see cref="DemoDataSeeder"/>: draait tegen een in-memory
/// database en bewijst dat de seeder (a) zonder exceptie draait — cruciaal omdat
/// hij bij het opstarten loopt — en (b) idempotent is: twee keer draaien levert
/// exact dezelfde aantallen op.
/// </summary>
public sealed class DemoDataSeederTests
{
    private sealed class VasteTenant : ITenantProvider
    {
        public Guid CurrentOrganisatieId => SeedConstanten.OrganisatieId;
    }

    private sealed class GeheugenOpslag : IBestandsopslag
    {
        public Task<string> OpslaanAsync(string map, string naam, Stream inhoud, CancellationToken ct = default)
            => Task.FromResult($"{map}/{Guid.NewGuid():N}-{naam}");

        public Task<Stream?> OpenenAsync(string sleutel, CancellationToken ct = default)
            => Task.FromResult<Stream?>(null);

        public Task VerwijderAsync(string sleutel, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private static KinderKompasDbContext MaakContext(string naam)
    {
        var opties = new DbContextOptionsBuilder<KinderKompasDbContext>()
            .UseInMemoryDatabase(naam)
            .Options;
        var db = new KinderKompasDbContext(opties, new VasteTenant());
        db.Database.EnsureCreated();
        return db;
    }

    private static DemoDataSeeder MaakSeeder(KinderKompasDbContext db)
        => new(db, new GeheugenOpslag(), NullLogger<DemoDataSeeder>.Instance);

    [Fact]
    public async Task Seeden_vult_alle_modules()
    {
        using var db = MaakContext(nameof(Seeden_vult_alle_modules));

        await MaakSeeder(db).SeedAsync();

        Assert.Equal(8, await db.Medewerkers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(12, await db.Kinderen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(16, await db.Verlofsaldi.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.Verlofaanvragen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.Ziekmeldingen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.Roosterweken.IgnoreQueryFilters().CountAsync());
        Assert.True(await db.Roosterdiensten.IgnoreQueryFilters().CountAsync() > 0);
        Assert.Equal(4, await db.Wachtlijstinschrijvingen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.Voorstellen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.VoorstelDagen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(7, await db.Observaties.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3, await db.Urenregistraties.IgnoreQueryFilters().CountAsync());
        Assert.Equal(5, await db.Meldingen.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Twee_keer_seeden_is_idempotent()
    {
        using var db = MaakContext(nameof(Twee_keer_seeden_is_idempotent));

        await MaakSeeder(db).SeedAsync();
        await MaakSeeder(db).SeedAsync();

        // Geen verdubbeling: dezelfde aantallen als na één keer seeden.
        Assert.Equal(8, await db.Medewerkers.IgnoreQueryFilters().CountAsync());
        Assert.Equal(12, await db.Kinderen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(16, await db.Verlofsaldi.IgnoreQueryFilters().CountAsync());
        Assert.Equal(4, await db.Wachtlijstinschrijvingen.IgnoreQueryFilters().CountAsync());
        Assert.Equal(7, await db.Observaties.IgnoreQueryFilters().CountAsync());
        Assert.Equal(5, await db.Meldingen.IgnoreQueryFilters().CountAsync());
    }
}
