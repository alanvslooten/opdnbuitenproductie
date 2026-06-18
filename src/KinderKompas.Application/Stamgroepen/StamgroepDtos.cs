using FluentValidation;
using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Stamgroepen;

/// <summary>Leesmodel van een stamgroep voor de API.</summary>
public sealed record StamgroepDto(
    Guid Id,
    string Naam,
    int MaxKinderen,
    int AantalKinderen);

/// <summary>Invoermodel voor het aanmaken/bewerken van een stamgroep.</summary>
public sealed record StamgroepInvoer(string Naam, int MaxKinderen);

/// <summary>
/// Validatie voor stamgroep-invoer. Het wettelijk maximum groepsgrootte in de
/// dagopvang is 16 (oudere leeftijden); een groep onder de 1 plaats heeft geen zin.
/// </summary>
public sealed class StamgroepInvoerValidator : AbstractValidator<StamgroepInvoer>
{
    public StamgroepInvoerValidator()
    {
        RuleFor(x => x.Naam)
            .NotEmpty().WithMessage("Een stamgroep moet een naam hebben.")
            .MaximumLength(100);

        RuleFor(x => x.MaxKinderen)
            .InclusiveBetween(1, 16)
            .WithMessage("Het maximum aantal kinderen ligt tussen 1 en 16.");
    }
}

/// <summary>Projecteert een <see cref="Stamgroep"/> naar een <see cref="StamgroepDto"/>.</summary>
public static class StamgroepMapper
{
    public static StamgroepDto NaarDto(Stamgroep stamgroep, int aantalKinderen) =>
        new(stamgroep.Id, stamgroep.Naam, stamgroep.MaxKinderen, aantalKinderen);
}
