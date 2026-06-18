using FluentValidation;
using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Schoolvakanties;

/// <summary>Leesmodel van een schoolvakantie voor de API.</summary>
public sealed record SchoolvakantieDto(
    Guid Id,
    string Naam,
    int Schooljaar,
    string SchooljaarLabel,
    DateOnly Begindatum,
    DateOnly Einddatum);

/// <summary>Invoermodel voor het aanmaken/bewerken van een schoolvakantie.</summary>
public sealed record SchoolvakantieInvoer(
    string Naam,
    int Schooljaar,
    DateOnly Begindatum,
    DateOnly Einddatum);

public sealed class SchoolvakantieInvoerValidator : AbstractValidator<SchoolvakantieInvoer>
{
    public SchoolvakantieInvoerValidator()
    {
        RuleFor(x => x.Naam).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Schooljaar)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Vul een geldig schooljaar in (beginjaar tussen 2000 en 2100).");

        RuleFor(x => x.Begindatum)
            .NotEqual(default(DateOnly)).WithMessage("Begindatum is verplicht.");

        RuleFor(x => x.Einddatum)
            .GreaterThanOrEqualTo(x => x.Begindatum)
            .WithMessage("De einddatum mag niet vóór de begindatum liggen.");
    }
}

public static class SchoolvakantieMapper
{
    public static SchoolvakantieDto NaarDto(Schoolvakantie vakantie) =>
        new(vakantie.Id, vakantie.Naam, vakantie.Schooljaar, vakantie.SchooljaarLabel,
            vakantie.Begindatum, vakantie.Einddatum);

    public static void PasInvoerToe(Schoolvakantie vakantie, SchoolvakantieInvoer invoer)
    {
        vakantie.Naam = invoer.Naam;
        vakantie.Schooljaar = invoer.Schooljaar;
        vakantie.Begindatum = invoer.Begindatum;
        vakantie.Einddatum = invoer.Einddatum;
    }
}
