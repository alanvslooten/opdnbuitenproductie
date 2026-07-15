using FluentValidation;
using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Kennisbank;

/// <summary>Leesmodel van één kennisbankdocument.</summary>
public sealed record KennisbankDocumentDto(
    Guid Id,
    string Titel,
    string? Categorie,
    string Inhoud,
    DateTime GewijzigdOp);

/// <summary>Kort leesmodel voor de lijst (zonder de volledige inhoud).</summary>
public sealed record KennisbankItemDto(
    Guid Id,
    string Titel,
    string? Categorie,
    DateTime GewijzigdOp);

/// <summary>Invoer voor het aanmaken/bijwerken van een kennisbankdocument (beheerder).</summary>
public sealed record KennisbankInvoer(string Titel, string? Categorie, string Inhoud);

public static class KennisbankMapper
{
    public static KennisbankDocumentDto NaarDto(KennisbankDocument d) =>
        new(d.Id, d.Titel, d.Categorie, d.Inhoud, d.GewijzigdOp);

    public static KennisbankItemDto NaarItem(KennisbankDocument d) =>
        new(d.Id, d.Titel, d.Categorie, d.GewijzigdOp);
}

/// <summary>Validatie voor kennisbank-invoer.</summary>
public sealed class KennisbankInvoerValidator : AbstractValidator<KennisbankInvoer>
{
    public KennisbankInvoerValidator()
    {
        RuleFor(x => x.Titel).NotEmpty().WithMessage("Een titel is verplicht.").MaximumLength(200);
        RuleFor(x => x.Categorie).MaximumLength(100);
        RuleFor(x => x.Inhoud).NotEmpty().WithMessage("Vul de inhoud van het document in.").MaximumLength(20000);
    }
}
