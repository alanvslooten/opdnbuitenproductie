using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Portaal;

/// <summary>Leesmodel van één in-/uitklok-registratie (Groepsportaal, fase 8).</summary>
public sealed record UrenregistratieDto(
    Guid Id,
    Guid MedewerkerId,
    string MedewerkerNaam,
    DateOnly Datum,
    Guid? RoosterdienstId,
    Guid? StamgroepId,
    string? StamgroepNaam,
    DateTime Ingeklokt,
    DateTime? Uitgeklokt,
    bool IsOpen,
    int GewerkteKwartieren);

/// <summary>
/// Invoer voor het inklokken op het tablet: de medewerker kiest zichzelf en bevestigt
/// met het eigen account-<see cref="Wachtwoord"/> (identiteitscheck — hetzelfde wachtwoord
/// als in het medewerkers-portaal, geen aparte pincode). Voorkomt dat iemand een collega klokt.
/// </summary>
public sealed record InklokInvoer(Guid MedewerkerId, Guid? RoosterdienstId, Guid? StamgroepId, string? Wachtwoord = null);

/// <summary>
/// Invoer voor het uitklokken. <see cref="Uitgeklokt"/> is optioneel: zonder waarde
/// telt het huidige tijdstip; mét waarde kan een vergeten uitklokmoment achteraf op de
/// juiste tijd worden gezet.
/// </summary>
public sealed record UitklokInvoer(DateTime? Uitgeklokt);

/// <summary>Beheerder-correctie van een urenregistratie: de in-/uitkloktijden achteraf zetten.</summary>
public sealed record UrencorrectieInvoer(DateTime Ingeklokt, DateTime? Uitgeklokt);

/// <summary>Projecteert een <see cref="Urenregistratie"/> naar zijn leesmodel.</summary>
public static class UrenregistratieMapper
{
    public static UrenregistratieDto NaarDto(Urenregistratie u, string medewerkerNaam, string? stamgroepNaam) =>
        new(
            u.Id,
            u.MedewerkerId,
            medewerkerNaam,
            u.Datum,
            u.RoosterdienstId,
            u.StamgroepId,
            stamgroepNaam,
            u.Ingeklokt,
            u.Uitgeklokt,
            u.IsOpen,
            u.GewerkteKwartieren);
}
