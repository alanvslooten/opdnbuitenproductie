namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// De contactgegevens van de ouder/verzorger van een kind. Bewust een
/// onveranderlijk value object: het hoort bij het kind en heeft geen eigen
/// identiteit. Privacy-gevoelig — zichtbaarheid wordt afgedwongen via de
/// capability <see cref="Autorisatie.Capabilities.MagOudergegevensZien"/> en
/// DTO-projectie (deze gegevens worden niet eens meegestuurd naar het
/// thuis-portaal).
/// </summary>
/// <param name="Rol">
/// De relatie tot het kind (bijv. "mama", "papa", "tante", "goede vriendin").
/// Optioneel; leeg als niet ingevuld. De eerste in de lijst blijft het primaire contact.
/// </param>
public sealed record Oudercontact(string Naam, string Telefoon, string Email, string Rol = "");
