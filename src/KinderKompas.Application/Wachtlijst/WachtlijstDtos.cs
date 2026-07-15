using FluentValidation;
using KinderKompas.Application.Kinderen;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>
/// Leesmodel van een wachtlijst-inschrijving voor de API, inclusief de afgeleide
/// prioriteitsscore (met onderbouwing) en de nog openstaande dagen. Privacy-kern:
/// <see cref="Oudercontact"/> is — net als bij <see cref="KindDto"/> — alleen
/// gevuld voor een aanroeper met de capability <c>MagOudergegevensZien</c>.
/// </summary>
public sealed record WachtlijstInschrijvingDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    DateOnly Geboortedatum,
    DateOnly InschrijfdatumWachtlijst,
    DateOnly GewensteStartdatum,
    Weekdag GewensteOpvangdagen,
    Weekdag OpenstaandeDagen,
    Weekdag ReedsGeplaatsteDagen,
    Contracttype Contracttype,
    Guid? GewensteStamgroepId,
    bool IsIntern,
    bool HandmatigBovenaan,
    WachtlijstStatus Status,
    // True als er een verstuurd, nog niet beantwoord voorstel openstaat: "voorstel verstuurd".
    bool HeeftOpenVoorstel,
    string? Notitie,
    int Prioriteitsscore,
    IReadOnlyList<string> PrioriteitOnderdelen,
    bool WordtBinnenkortVier,
    OudercontactDto? Oudercontact);

/// <summary>Invoermodel voor het aanmaken/bewerken van een wachtlijst-inschrijving.</summary>
public sealed record WachtlijstInvoer(
    string Voornaam,
    string Achternaam,
    DateOnly Geboortedatum,
    DateOnly InschrijfdatumWachtlijst,
    DateOnly GewensteStartdatum,
    Weekdag GewensteOpvangdagen,
    Contracttype Contracttype,
    Guid? GewensteStamgroepId,
    bool IsIntern,
    bool HandmatigBovenaan,
    string? Notitie,
    OudercontactDto? Oudercontact);

/// <summary>
/// Validatie voor wachtlijst-invoer. Bewaakt de vorm; de bestaat-de-stamgroep- en
/// tenant-controles vereisen de database en gebeuren in de controller.
/// </summary>
public sealed class WachtlijstInvoerValidator : AbstractValidator<WachtlijstInvoer>
{
    public WachtlijstInvoerValidator()
    {
        RuleFor(x => x.Voornaam).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Achternaam).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Geboortedatum)
            .NotEqual(default(DateOnly)).WithMessage("Geboortedatum is verplicht.");

        RuleFor(x => x.InschrijfdatumWachtlijst)
            .NotEqual(default(DateOnly)).WithMessage("Inschrijfdatum is verplicht.");

        RuleFor(x => x.GewensteStartdatum)
            .NotEqual(default(DateOnly)).WithMessage("Gewenste startdatum is verplicht.");

        RuleFor(x => x.GewensteOpvangdagen)
            .NotEqual(Weekdag.Geen).WithMessage("Kies minstens één gewenste opvangdag.");

        RuleFor(x => x.Contracttype).IsInEnum();

        When(x => x.Oudercontact is not null, () =>
        {
            RuleFor(x => x.Oudercontact!.Naam).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Oudercontact!.Telefoon).MaximumLength(30);
            RuleFor(x => x.Oudercontact!.Email)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Oudercontact!.Email))
                .MaximumLength(200);
        });
    }
}
