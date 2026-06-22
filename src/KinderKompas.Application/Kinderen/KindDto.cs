using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Kinderen;

/// <summary>
/// Leesmodel van een kind voor de API. Privacy-kern: <see cref="Oudercontact"/>
/// is alléén gevuld voor een aanroeper met de capability
/// <c>MagOudergegevensZien</c> (Groepsportaal op locatie / Beheerder). Voor het
/// thuis-portaal blijft het null en wordt het — dankzij null-weglating in de
/// JSON-serializer — niet eens over de lijn gestuurd.
/// </summary>
public sealed record KindDto(
    Guid Id,
    string Voornaam,
    string Achternaam,
    DateOnly Geboortedatum,
    Guid StamgroepId,
    DateOnly Startdatum,
    DateOnly? Einddatum,
    DateOnly EffectieveEinddatum,
    Contracttype Contracttype,
    Weekdag GewensteOpvangdagen,
    bool WordtBinnenkortVier,
    Guid? MentorId,
    IReadOnlyList<OudercontactDto> Oudercontacten);

public sealed record OudercontactDto(string Naam, string Telefoon, string Email);

/// <summary>
/// Invoermodel voor het aanmaken/bewerken van een kind. <see cref="Einddatum"/> is
/// optioneel: blijft die leeg, dan geldt wettelijk de vierde verjaardag als
/// einddatum (het kind stroomt dan door naar de BSO/school).
/// </summary>
public sealed record KindInvoer(
    string Voornaam,
    string Achternaam,
    DateOnly Geboortedatum,
    Guid StamgroepId,
    DateOnly Startdatum,
    DateOnly? Einddatum,
    Contracttype Contracttype,
    Weekdag GewensteOpvangdagen,
    Guid? MentorId,
    IReadOnlyList<OudercontactDto> Oudercontacten);
