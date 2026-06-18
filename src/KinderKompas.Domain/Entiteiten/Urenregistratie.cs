using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén in-/uitklok-registratie van een medewerker op het Groepsportaal (fase 8).
/// Het tablet is een gedeeld account: de medewerker kiest zichzelf en klokt in;
/// bij uitklokken wordt de werkelijk gewerkte tijd vastgelegd. Dit staat LOS van
/// de planner-correctie op de <see cref="Roosterdienst"/> (gepland vs. werkelijk):
/// de optionele koppeling <see cref="RoosterdienstId"/> verbindt de werkelijke uren
/// met de geplande dienst van die dag.
/// </summary>
public class Urenregistratie : TenantEntiteit
{
    public Guid MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }

    /// <summary>De geplande dienst waaraan deze registratie hangt (optioneel: ad-hoc inzet kan zonder dienst).</summary>
    public Guid? RoosterdienstId { get; set; }
    public Roosterdienst? Roosterdienst { get; set; }

    /// <summary>De stamgroep waar op locatie geklokt is (optioneel; context van het portaal).</summary>
    public Guid? StamgroepId { get; set; }
    public Stamgroep? Stamgroep { get; set; }

    /// <summary>De opvangdag van deze registratie.</summary>
    public DateOnly Datum { get; set; }

    /// <summary>Tijdstip (UTC) van inklokken. Altijd gezet zodra de registratie bestaat.</summary>
    public DateTime Ingeklokt { get; set; }

    /// <summary>Tijdstip (UTC) van uitklokken. Null zolang de medewerker nog ingeklokt is.</summary>
    public DateTime? Uitgeklokt { get; set; }

    /// <summary>True zolang er nog niet is uitgeklokt.</summary>
    public bool IsOpen => Uitgeklokt is null;

    /// <summary>
    /// De gewerkte tijd geteld in hele kwartieren (afgerond op het dichtstbijzijnde
    /// kwartier). 0 zolang er nog niet is uitgeklokt. Zo blijft de registratie in
    /// kwartieren zonder afrondingsproblemen op decimalen — net als de
    /// urencorrectie op de <see cref="Roosterdienst"/>.
    /// </summary>
    public int GewerkteKwartieren =>
        Uitgeklokt is { } uit && uit > Ingeklokt
            ? (int)Math.Round((uit - Ingeklokt).TotalMinutes / 15.0, MidpointRounding.AwayFromZero)
            : 0;

    /// <summary>De gewerkte tijd in uren (kwartieren / 4).</summary>
    public decimal GewerkteUren => GewerkteKwartieren / 4m;
}
