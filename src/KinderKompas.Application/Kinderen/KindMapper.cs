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
        OudercontactDto? oudercontact =
            gebruiker.Heeft(Capabilities.MagOudergegevensZien) && kind.Oudercontact is not null
                ? new OudercontactDto(
                    kind.Oudercontact.Naam,
                    kind.Oudercontact.Telefoon,
                    kind.Oudercontact.Email)
                : null;

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
            oudercontact);
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
        kind.Oudercontact = invoer.Oudercontact is null
            ? null
            : new Oudercontact(
                invoer.Oudercontact.Naam,
                invoer.Oudercontact.Telefoon,
                invoer.Oudercontact.Email);
    }
}
