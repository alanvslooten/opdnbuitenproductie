using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Meldingen;
using KinderKompas.Application.Observaties;
using KinderKompas.Application.Wachtlijst;
using KinderKompas.Infrastructure.Bestandsopslag;
using KinderKompas.Infrastructure.Identity;
using KinderKompas.Infrastructure.Instellingen;
using KinderKompas.Infrastructure.Meldingen;
using KinderKompas.Infrastructure.Observaties;
using KinderKompas.Infrastructure.Persistence;
using KinderKompas.Infrastructure.Wachtlijst;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KinderKompas.Infrastructure;

/// <summary>
/// Registreert de Infrastructure-diensten (EF Core, Identity, auth-services) in
/// de DI-container. De tenant-provider en de current-user worden in de Api-laag
/// geregistreerd: die zijn HTTP-/claim-gebonden.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Npgsql met "modern" UTC-gedrag levert timestamptz op en eist Kind=Utc.
        // Al onze tijdstempels zijn UTC, maar we zetten het legacy-gedrag aan zodat
        // een eventuele niet-UTC DateTime nooit een runtime-exception veroorzaakt.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        string ruweConnectie = configuration.GetConnectionString("KinderKompas")
            ?? throw new InvalidOperationException(
                "Connection string 'KinderKompas' ontbreekt. Stel deze in via user-secrets (lokaal) of een environment variable (productie).");

        // Render levert de databaseverbinding als URL ('postgres://user:pass@host/db');
        // Npgsql verwacht een key-value string. Normaliseer beide vormen.
        string connectionString = PostgresVerbinding.Normaliseer(ruweConnectie);

        services.AddDbContext<KinderKompasDbContext>(options =>
            options.UseNpgsql(connectionString, npg =>
                npg.MigrationsAssembly(typeof(KinderKompasDbContext).Assembly.FullName)));

        AddIdentityEnAuth(services, configuration);

        // Actiecentrum (fase 9): meldingen/to-do's uit domein-events.
        services.AddScoped<IMeldingDispatcher, MeldingDispatcher>();

        // Instelbaar moduledgedrag per organisatie (fase 9c).
        services.AddScoped<IInstellingenProvider, InstellingenProvider>();

        // Trigger-punt voor de contract-to-do na acceptatie: maakt nu een echte to-do aan.
        services.AddScoped<IPlaatsingsToDo, MeldingPlaatsingsToDo>();

        // Observatie-opslag (lokaal nu, Azure Blob later) + mailer-stub.
        services.Configure<BestandsopslagOptions>(configuration.GetSection(BestandsopslagOptions.Sectie));
        services.AddScoped<IBestandsopslag, LokaleBestandsopslag>();
        services.AddScoped<IObservatieMailer, ObservatieMailerStub>();

        return services;
    }

    private static void AddIdentityEnAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Sectie));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<KinderKompasDbContext>()
            // Alleen de authenticator-provider (TOTP) is nodig voor 2FA. De volledige
            // AddDefaultTokenProviders() leeft in het ASP.NET-framework-assembly dat
            // deze classlib niet referenceert; expliciet registreren volstaat.
            .AddTokenProvider<AuthenticatorTokenProvider<ApplicationUser>>(
                TokenOptions.DefaultAuthenticatorProvider);

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IdentityDataSeeder>();
        services.AddScoped<DemoDataSeeder>();
    }
}
