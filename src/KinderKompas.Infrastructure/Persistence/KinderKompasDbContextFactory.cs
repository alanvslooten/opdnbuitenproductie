using KinderKompas.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Design-time factory voor <c>dotnet ef</c> (migraties genereren/toepassen vanaf
/// de CLI). Hiermee bouwt EF de context zónder de volledige web-host te starten —
/// dus zonder seeding of een echte databaseverbinding. De verbindingsstring hier is
/// alleen nodig om de Npgsql-provider te kiezen; migraties genereren raakt de DB niet.
/// </summary>
public sealed class KinderKompasDbContextFactory : IDesignTimeDbContextFactory<KinderKompasDbContext>
{
    public KinderKompasDbContext CreateDbContext(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var options = new DbContextOptionsBuilder<KinderKompasDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=kinderkompas_design;Username=postgres;Password=postgres",
                npg => npg.MigrationsAssembly(typeof(KinderKompasDbContext).Assembly.FullName))
            .Options;

        return new KinderKompasDbContext(options, new OntwerpTenantProvider());
    }

    /// <summary>Vaste tenant; alleen nodig om de queryfilters bij modelopbouw te laten compileren.</summary>
    private sealed class OntwerpTenantProvider : ITenantProvider
    {
        public Guid CurrentOrganisatieId => SeedConstanten.OrganisatieId;
    }
}
