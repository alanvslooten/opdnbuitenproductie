using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Autorisatie;

/// <summary>
/// De anti-uitsluiting-regel voor de bewerkbare rechtenmatrix (fase 9c). Voorkomt dat
/// de Beheerder zichzelf — en daarmee iedereen — buitensluit van het beheer: de rol
/// <see cref="Rol.Beheerder"/> moet altijd minimaal de instellingen kunnen beheren
/// (anders is de matrix nooit meer te herstellen) en het dashboard/actiecentrum zien.
/// Pure regel, los van database en UI, zodat hij eenduidig getest kan worden.
/// </summary>
public static class RechtenVangrail
{
    /// <summary>De capabilities die de Beheerder nooit kan verliezen.</summary>
    public static readonly IReadOnlyList<string> BeheerderBeschermd = new[]
    {
        Capabilities.MagInstellingenBeheren,
        Capabilities.MagDashboardZien,
    };

    /// <summary>
    /// De beschermde capabilities die in de gevraagde set voor een rol ontbreken. Voor
    /// elke rol behalve <see cref="Rol.Beheerder"/> is dit altijd leeg (geen vangrail).
    /// Een niet-lege uitkomst betekent: de wijziging moet geweigerd worden.
    /// </summary>
    public static IReadOnlyList<string> OntbrekendeBeschermdeRechten(Rol rol, IEnumerable<string> gevraagdeSleutels)
    {
        ArgumentNullException.ThrowIfNull(gevraagdeSleutels);

        if (rol != Rol.Beheerder)
        {
            return Array.Empty<string>();
        }

        var gevraagd = gevraagdeSleutels.ToHashSet();
        return BeheerderBeschermd.Where(sleutel => !gevraagd.Contains(sleutel)).ToList();
    }
}
