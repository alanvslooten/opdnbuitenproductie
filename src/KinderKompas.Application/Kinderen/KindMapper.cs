using KinderKompas.Application.Abstractions;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Kinderen;

/// <summary>
/// Projecteert een <see cref="Kind"/> naar een <see cref="KindDto"/> en dwingt
/// daarbij de privacy-scheiding af: oudergegevens komen alleen in het DTO als de
/// aanroeper de capability <see cref="Capabilities.MagOudergegevensZien"/> bezit.
/// De beslissing zit hier (in de Application-laag), niet in de controller of de
/// frontend, en is daarom puur unit-testbaar.
/// </summary>
public static class KindMapper
{
    public static KindDto NaarDto(Kind kind, ICurrentUser gebruiker, DateOnly peildatum)
    {
        IReadOnlyList<OudercontactDto> oudercontacten =
            gebruiker.Heeft(Capabilities.MagOudergegevensZien)
                ? kind.Oudercontacten
                    .Select(o => new OudercontactDto(o.Naam, o.Telefoon, o.Email, o.Rol))
                    .ToList()
                : [];

        return new KindDto(
            kind.Id,
            kind.Voornaam,
            kind.Achternaam,
            kind.Geboortedatum,
            kind.StamgroepId,
            kind.Startdatum,
            kind.Einddatum,
            kind.EffectieveEinddatum,
            kind.Contracttype,
            kind.GewensteOpvangdagen,
            kind.WordtBinnenkortVier(peildatum),
            kind.MentorId,
            oudercontacten);
    }

    /// <summary>Zet de waarden uit een invoermodel op een (nieuw of bestaand) kind.</summary>
    public static void PasInvoerToe(Kind kind, KindInvoer invoer)
    {
        kind.Voornaam = invoer.Voornaam;
        kind.Achternaam = invoer.Achternaam;
        kind.Geboortedatum = invoer.Geboortedatum;
        kind.StamgroepId = invoer.StamgroepId;
        kind.Startdatum = invoer.Startdatum;
        kind.Einddatum = invoer.Einddatum;
        kind.Contracttype = invoer.Contracttype;
        kind.GewensteOpvangdagen = invoer.GewensteOpvangdagen;
        kind.MentorId = invoer.MentorId;
        // Volledige vervanging: de lijst uit de invoer is de nieuwe waarheid (lege
        // velden eruit gefilterd zodat een leeg formulier geen leeg contact opslaat).
        kind.Oudercontacten = invoer.Oudercontacten
            .Where(o => !string.IsNullOrWhiteSpace(o.Naam) || !string.IsNullOrWhiteSpace(o.Telefoon) || !string.IsNullOrWhiteSpace(o.Email))
            .Select(o => new Oudercontact(o.Naam, o.Telefoon, o.Email, o.Rol ?? ""))
            .ToList();
    }
}
