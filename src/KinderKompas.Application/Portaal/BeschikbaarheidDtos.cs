using FluentValidation;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Portaal;

/// <summary>De eigen roosterlagen van een medewerker in het thuis-portaal.</summary>
public sealed record BeschikbaarheidDto(
    Guid MedewerkerId,
    Weekdag VasteWerkdagen,
    Weekdag Beschikbaarheidsdagen);

/// <summary>
/// Invoer waarmee de medewerker zélf z'n beschikbaarheidsdagen opgeeft. De vaste
/// werkdagen blijven van de planner; daarom mogen ze hier niet overlappen — die
/// overlapcontrole gebeurt in de controller (die de vaste dagen kent).
/// </summary>
public sealed record BeschikbaarheidInvoer(Weekdag Beschikbaarheidsdagen);

/// <summary>Valideert dat opgegeven beschikbaarheid alleen geldige opvangdagen (ma-vr) bevat.</summary>
public sealed class BeschikbaarheidInvoerValidator : AbstractValidator<BeschikbaarheidInvoer>
{
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    public BeschikbaarheidInvoerValidator()
    {
        RuleFor(x => x.Beschikbaarheidsdagen)
            .Must(d => (d & ~AlleWeekdagen) == Weekdag.Geen)
            .WithMessage("Beschikbaarheidsdagen bevatten een ongeldige dag (alleen ma-vr).");
    }
}
