using FluentValidation;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Medewerkers;

/// <summary>Leesmodel van een medewerker voor de API.</summary>
public sealed record MedewerkerDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    Rol Rol,
    Weekdag VasteWerkdagen,
    Weekdag Beschikbaarheidsdagen,
    decimal Contracturen,
    Guid? VasteStamgroepId,
    string? VasteStamgroepNaam);

/// <summary>Invoermodel voor het aanmaken/bewerken van een medewerker.</summary>
public sealed record MedewerkerInvoer(
    string Voornaam,
    string Achternaam,
    Rol Rol,
    Weekdag VasteWerkdagen,
    Weekdag Beschikbaarheidsdagen,
    decimal Contracturen,
    Guid? VasteStamgroepId);

/// <summary>
/// Validatie voor medewerker-invoer. De roosterlagen mogen elkaar niet overlappen:
/// een vaste werkdag is al ingepland en kan niet ook "beschikbaar bij uitval" zijn.
/// </summary>
public sealed class MedewerkerInvoerValidator : AbstractValidator<MedewerkerInvoer>
{
    // Alle geldige opvang-weekdagvlaggen samen (ma t/m vr).
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    public MedewerkerInvoerValidator()
    {
        RuleFor(x => x.Voornaam)
            .NotEmpty().WithMessage("Een medewerker moet een voornaam hebben.")
            .MaximumLength(100);

        RuleFor(x => x.Achternaam)
            .NotEmpty().WithMessage("Een medewerker moet een achternaam hebben.")
            .MaximumLength(100);

        RuleFor(x => x.Rol)
            .IsInEnum().WithMessage("Onbekende rol.");

        RuleFor(x => x.Contracturen)
            .InclusiveBetween(0m, 40m)
            .WithMessage("Contracturen per week liggen tussen 0 en 40.");

        RuleFor(x => x.VasteWerkdagen)
            .Must(GeenOnbekendeDagen).WithMessage("Vaste werkdagen bevatten een ongeldige dag (alleen ma-vr).");

        RuleFor(x => x.Beschikbaarheidsdagen)
            .Must(GeenOnbekendeDagen).WithMessage("Beschikbaarheidsdagen bevatten een ongeldige dag (alleen ma-vr).");

        RuleFor(x => x)
            .Must(x => (x.VasteWerkdagen & x.Beschikbaarheidsdagen) == Weekdag.Geen)
            .WithMessage("Een dag kan niet tegelijk vaste werkdag én beschikbaarheidsdag zijn.")
            .WithName(nameof(MedewerkerInvoer.Beschikbaarheidsdagen));
    }

    private static bool GeenOnbekendeDagen(Weekdag dagen) => (dagen & ~AlleWeekdagen) == Weekdag.Geen;
}

/// <summary>Projecteert een <see cref="Medewerker"/> naar een <see cref="MedewerkerDto"/>.</summary>
public static class MedewerkerMapper
{
    public static MedewerkerDto NaarDto(Medewerker m) =>
        new(m.Id, m.Voornaam, m.Achternaam, m.Rol, m.VasteWerkdagen, m.Beschikbaarheidsdagen,
            m.Contracturen, m.VasteStamgroepId, m.VasteStamgroep?.Naam);
}
