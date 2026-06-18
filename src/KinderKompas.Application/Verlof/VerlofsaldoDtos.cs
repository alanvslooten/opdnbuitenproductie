using FluentValidation;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Verlof;

/// <summary>Leesmodel van een berekende verlofsaldo-stand voor de API.</summary>
public sealed record VerlofsaldoDto(
    Guid MedewerkerId,
    VerlofCategorie Categorie,
    decimal Toegekend,
    decimal Gebruikt,
    decimal Gereserveerd,
    decimal Resterend,
    decimal ResterendNaReservering,
    DateOnly? Vervaldatum);

/// <summary>Invoer voor het instellen (upsert) van een toegekend saldo per categorie.</summary>
public sealed record VerlofsaldoInvoer(
    Guid MedewerkerId,
    VerlofCategorie Categorie,
    decimal ToegekendeUren,
    DateOnly? Vervaldatum);

public sealed class VerlofsaldoInvoerValidator : AbstractValidator<VerlofsaldoInvoer>
{
    public VerlofsaldoInvoerValidator()
    {
        RuleFor(x => x.MedewerkerId).NotEmpty();
        RuleFor(x => x.Categorie).IsInEnum().WithMessage("Onbekende verlofcategorie.");
        RuleFor(x => x.ToegekendeUren)
            .GreaterThanOrEqualTo(0m).WithMessage("Toegekende uren kunnen niet negatief zijn.");
    }
}

public static class VerlofsaldoMapper
{
    public static VerlofsaldoDto NaarDto(Guid medewerkerId, Verlofsaldostand stand) =>
        new(medewerkerId, stand.Categorie, stand.Toegekend, stand.Gebruikt, stand.Gereserveerd,
            stand.Resterend, stand.ResterendNaReservering, stand.Vervaldatum);
}
