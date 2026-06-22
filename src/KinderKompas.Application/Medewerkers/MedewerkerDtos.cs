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
    string? VasteStamgroepNaam,
    string? Telefoon = null,
    string? Email = null,
    string? NoodcontactNaam = null,
    string? NoodcontactTelefoon = null,
    bool ContractVast = false,
    DateOnly? Contractbegindatum = null,
    DateOnly? Contracteinddatum = null,
    int? ResterendeContractmaanden = null,
    bool HeeftPincode = false);

/// <summary>Invoermodel voor het aanmaken/bewerken van een medewerker.</summary>
public sealed record MedewerkerInvoer(
    string Voornaam,
    string Achternaam,
    Rol Rol,
    Weekdag VasteWerkdagen,
    Weekdag Beschikbaarheidsdagen,
    decimal Contracturen,
    Guid? VasteStamgroepId,
    string? Telefoon = null,
    string? Email = null,
    string? NoodcontactNaam = null,
    string? NoodcontactTelefoon = null,
    bool ContractVast = false,
    DateOnly? Contractbegindatum = null,
    DateOnly? Contracteinddatum = null,
    string? Pincode = null);

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
    public static MedewerkerDto NaarDto(Medewerker m)
    {
        DateOnly vandaag = DateOnly.FromDateTime(DateTime.UtcNow);
        return new(m.Id, m.Voornaam, m.Achternaam, m.Rol, m.VasteWerkdagen, m.Beschikbaarheidsdagen,
            m.Contracturen, m.VasteStamgroepId, m.VasteStamgroep?.Naam,
            m.Telefoon, m.Email, m.NoodcontactNaam, m.NoodcontactTelefoon,
            m.ContractVast, m.Contractbegindatum, m.Contracteinddatum,
            m.ResterendeContractmaanden(vandaag),
            !string.IsNullOrEmpty(m.Pincode));
    }

    /// <summary>Zet de invoer op een (nieuw of bestaand) medewerker-record.</summary>
    public static void PasInvoerToe(Medewerker m, MedewerkerInvoer invoer)
    {
        m.Voornaam = invoer.Voornaam;
        m.Achternaam = invoer.Achternaam;
        m.Rol = invoer.Rol;
        m.VasteWerkdagen = invoer.VasteWerkdagen;
        m.Beschikbaarheidsdagen = invoer.Beschikbaarheidsdagen;
        m.Contracturen = invoer.Contracturen;
        m.VasteStamgroepId = invoer.VasteStamgroepId;
        m.Telefoon = Leeg(invoer.Telefoon);
        m.Email = Leeg(invoer.Email);
        m.NoodcontactNaam = Leeg(invoer.NoodcontactNaam);
        m.NoodcontactTelefoon = Leeg(invoer.NoodcontactTelefoon);
        m.ContractVast = invoer.ContractVast;
        m.Contractbegindatum = invoer.Contractbegindatum;
        m.Contracteinddatum = invoer.ContractVast ? null : invoer.Contracteinddatum;
        // Pincode alleen overschrijven als er een (niet-lege) waarde is meegegeven.
        if (!string.IsNullOrWhiteSpace(invoer.Pincode))
        {
            m.Pincode = invoer.Pincode.Trim();
        }
    }

    private static string? Leeg(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
