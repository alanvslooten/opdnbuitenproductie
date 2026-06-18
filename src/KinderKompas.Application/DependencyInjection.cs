using FluentValidation;
using KinderKompas.Application.Instellingen;
using KinderKompas.Application.Kinderen;
using KinderKompas.Application.Medewerkers;
using KinderKompas.Application.Portaal;
using KinderKompas.Application.Schoolvakanties;
using KinderKompas.Application.Stamgroepen;
using KinderKompas.Application.Verlof;
using KinderKompas.Application.Wachtlijst;
using Microsoft.Extensions.DependencyInjection;

namespace KinderKompas.Application;

/// <summary>
/// Registreert de Application-diensten (de FluentValidation-validators) in de
/// DI-container. De validators worden per request in de controllers geïnjecteerd
/// via <see cref="IValidator{T}"/>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<StamgroepInvoer>, StamgroepInvoerValidator>();
        services.AddScoped<IValidator<KindInvoer>, KindInvoerValidator>();
        services.AddScoped<IValidator<SchoolvakantieInvoer>, SchoolvakantieInvoerValidator>();
        services.AddScoped<IValidator<MedewerkerInvoer>, MedewerkerInvoerValidator>();
        services.AddScoped<IValidator<VerlofAanvraagInvoer>, VerlofAanvraagInvoerValidator>();
        services.AddScoped<IValidator<ZiekmeldingInvoer>, ZiekmeldingInvoerValidator>();
        services.AddScoped<IValidator<VerlofsaldoInvoer>, VerlofsaldoInvoerValidator>();
        services.AddScoped<IValidator<WachtlijstInvoer>, WachtlijstInvoerValidator>();
        services.AddScoped<IValidator<VoorstelInvoer>, VoorstelInvoerValidator>();
        services.AddScoped<IValidator<BeschikbaarheidInvoer>, BeschikbaarheidInvoerValidator>();
        services.AddScoped<IValidator<InstellingenInvoer>, InstellingenInvoerValidator>();
        services.AddScoped<IValidator<LocatieInvoer>, LocatieInvoerValidator>();
        return services;
    }
}
