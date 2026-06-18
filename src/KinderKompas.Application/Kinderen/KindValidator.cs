using FluentValidation;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Kinderen;

/// <summary>
/// Validatie voor kind-invoer. Bewaakt de vorm; de groepsplaats-regel (max. 12,
/// de "13e plaatsing") en de tenant-scope vereisen de database en worden in de
/// controller/use-case afgehandeld.
/// </summary>
public sealed class KindInvoerValidator : AbstractValidator<KindInvoer>
{
    public KindInvoerValidator()
    {
        RuleFor(x => x.Voornaam).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Achternaam).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Geboortedatum)
            .NotEqual(default(DateOnly)).WithMessage("Geboortedatum is verplicht.");

        RuleFor(x => x.StamgroepId)
            .NotEqual(Guid.Empty).WithMessage("Een kind moet aan een stamgroep gekoppeld zijn.");

        RuleFor(x => x.Startdatum)
            .NotEqual(default(DateOnly)).WithMessage("Startdatum is verplicht.");

        // Einddatum optioneel, maar als ze is ingevuld nooit vóór de startdatum.
        RuleFor(x => x.Einddatum)
            .Must((invoer, einddatum) => einddatum is null || einddatum >= invoer.Startdatum)
            .WithMessage("De einddatum mag niet vóór de startdatum liggen.");

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
