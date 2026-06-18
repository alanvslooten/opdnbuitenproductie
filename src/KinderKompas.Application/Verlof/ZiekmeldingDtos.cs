using FluentValidation;
using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Verlof;

/// <summary>Leesmodel van een ziekmelding voor de API.</summary>
public sealed record ZiekmeldingDto(
    Guid Id,
    Guid MedewerkerId,
    string MedewerkerNaam,
    DateOnly Begindatum,
    DateOnly? Einddatum);

/// <summary>Invoer voor het registreren van een ziekmelding (einddatum optioneel/open).</summary>
public sealed record ZiekmeldingInvoer(
    Guid MedewerkerId,
    DateOnly Begindatum,
    DateOnly? Einddatum);

/// <summary>Invoer voor het beter melden: de hersteldatum (laatste ziektedag).</summary>
public sealed record ZiekHerstelInvoer(DateOnly Einddatum);

public sealed class ZiekmeldingInvoerValidator : AbstractValidator<ZiekmeldingInvoer>
{
    public ZiekmeldingInvoerValidator()
    {
        RuleFor(x => x.MedewerkerId).NotEmpty().WithMessage("Een ziekmelding hoort bij een medewerker.");

        RuleFor(x => x.Begindatum).NotEqual(default(DateOnly)).WithMessage("Begindatum is verplicht.");

        RuleFor(x => x.Einddatum!.Value)
            .GreaterThanOrEqualTo(x => x.Begindatum)
            .When(x => x.Einddatum is not null)
            .WithMessage("De einddatum mag niet vóór de begindatum liggen.");
    }
}

public static class ZiekmeldingMapper
{
    public static ZiekmeldingDto NaarDto(Ziekmelding z, string medewerkerNaam) =>
        new(z.Id, z.MedewerkerId, medewerkerNaam, z.Begindatum, z.Einddatum);
}
