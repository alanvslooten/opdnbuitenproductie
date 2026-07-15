using FluentValidation;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Publiek;

/// <summary>
/// Publieke wachtlijst-aanmelding: ouders melden hun kind zelf aan (vervangt de
/// Portabase-instroom). Bewust een BEPERKTE set velden — geen prioriteitsvlaggen,
/// geen stamgroepkeuze; die bepaalt de organisatie zelf na binnenkomst.
/// </summary>
public sealed record PubliekeAanmeldingInvoer(
    string Voornaam,
    string Achternaam,
    DateOnly Geboortedatum,
    DateOnly GewensteStartdatum,
    Weekdag GewensteOpvangdagen,
    Contracttype Contracttype,
    string OuderNaam,
    string OuderTelefoon,
    string OuderEmail,
    string? Opmerking);

/// <summary>Publieke rondleiding-aanvraag via de website.</summary>
public sealed record PubliekeRondleidingInvoer(
    string OuderVoornaam,
    string OuderAchternaam,
    string Telefoon,
    string Email,
    DateOnly VoorkeurDatum,
    string? Opmerking);

public sealed class PubliekeAanmeldingInvoerValidator : AbstractValidator<PubliekeAanmeldingInvoer>
{
    public PubliekeAanmeldingInvoerValidator()
    {
        RuleFor(x => x.Voornaam).NotEmpty().WithMessage("Vul de voornaam van het kind in.").MaximumLength(100);
        RuleFor(x => x.Achternaam).NotEmpty().WithMessage("Vul de achternaam van het kind in.").MaximumLength(100);
        RuleFor(x => x.Geboortedatum).NotEqual(default(DateOnly)).WithMessage("Vul de geboortedatum in.");
        RuleFor(x => x.GewensteStartdatum).NotEqual(default(DateOnly)).WithMessage("Vul de gewenste startdatum in.");
        RuleFor(x => x.GewensteOpvangdagen).NotEqual(Weekdag.Geen).WithMessage("Kies minstens één gewenste opvangdag.");
        RuleFor(x => x.OuderNaam).NotEmpty().WithMessage("Vul uw naam in.").MaximumLength(200);
        RuleFor(x => x.OuderTelefoon).NotEmpty().WithMessage("Vul een telefoonnummer in.").MaximumLength(30);
        RuleFor(x => x.OuderEmail).NotEmpty().EmailAddress().WithMessage("Vul een geldig e-mailadres in.").MaximumLength(200);
        RuleFor(x => x.Opmerking).MaximumLength(1000);
    }
}

public sealed class PubliekeRondleidingInvoerValidator : AbstractValidator<PubliekeRondleidingInvoer>
{
    public PubliekeRondleidingInvoerValidator()
    {
        RuleFor(x => x.OuderVoornaam).NotEmpty().WithMessage("Vul uw voornaam in.").MaximumLength(100);
        RuleFor(x => x.OuderAchternaam).NotEmpty().WithMessage("Vul uw achternaam in.").MaximumLength(100);
        RuleFor(x => x.Telefoon).NotEmpty().WithMessage("Vul een telefoonnummer in.").MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Vul een geldig e-mailadres in.").MaximumLength(200);
        RuleFor(x => x.VoorkeurDatum).NotEqual(default(DateOnly)).WithMessage("Kies een voorkeursdatum.");
        RuleFor(x => x.Opmerking).MaximumLength(1000);
    }
}
