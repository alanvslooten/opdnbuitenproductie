using FluentValidation;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Verlof;

/// <summary>Leesmodel van een verlofaanvraag voor de API.</summary>
public sealed record VerlofaanvraagDto(
    Guid Id,
    Guid MedewerkerId,
    string MedewerkerNaam,
    DateOnly Begindatum,
    DateOnly Einddatum,
    decimal AantalUren,
    VerlofCategorie Categorie,
    VerlofStatus Status,
    string? Reden,
    string? BeoordelingsNotitie,
    DateTime AangevraagdOp,
    DateTime? BeoordeeldOp);

/// <summary>Invoer voor het indienen van een verlofaanvraag.</summary>
public sealed record VerlofAanvraagInvoer(
    Guid MedewerkerId,
    DateOnly Begindatum,
    DateOnly Einddatum,
    decimal AantalUren,
    VerlofCategorie Categorie,
    string? Reden);

/// <summary>Invoer voor het afkeuren van een aanvraag (optionele toelichting).</summary>
public sealed record VerlofBeoordelingInvoer(string? Notitie);

public sealed class VerlofAanvraagInvoerValidator : AbstractValidator<VerlofAanvraagInvoer>
{
    public VerlofAanvraagInvoerValidator()
    {
        RuleFor(x => x.MedewerkerId).NotEmpty().WithMessage("Een verlofaanvraag hoort bij een medewerker.");

        RuleFor(x => x.Begindatum).NotEqual(default(DateOnly)).WithMessage("Begindatum is verplicht.");

        RuleFor(x => x.Einddatum)
            .GreaterThanOrEqualTo(x => x.Begindatum)
            .WithMessage("De einddatum mag niet vóór de begindatum liggen.");

        RuleFor(x => x.AantalUren)
            .GreaterThan(0m).WithMessage("Het aantal verlofuren moet groter dan 0 zijn.");

        RuleFor(x => x.Categorie).IsInEnum().WithMessage("Onbekende verlofcategorie.");

        RuleFor(x => x.Reden).MaximumLength(500);
    }
}

public static class VerlofaanvraagMapper
{
    public static VerlofaanvraagDto NaarDto(Verlofaanvraag a, string medewerkerNaam) =>
        new(a.Id, a.MedewerkerId, medewerkerNaam, a.Begindatum, a.Einddatum, a.AantalUren,
            a.Categorie, a.Status, a.Reden, a.BeoordelingsNotitie, a.AangemaaktOp, a.BeoordeeldOp);
}
