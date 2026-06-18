using KinderKompas.Application.Auth;
using KinderKompas.Domain.Autorisatie;
using Microsoft.AspNetCore.Authorization;

namespace KinderKompas.Api.Auth;

/// <summary>
/// Registreert één autorisatie-policy per capability. Een endpoint eist een recht
/// met <c>[Authorize(Policy = Capabilities.MagX)]</c>; de policy controleert of de
/// token een bijbehorende <c>cap</c>-claim bevat.
///
/// De capabilities zitten als claims in het JWT (bij login/refresh afgeleid uit de
/// data-gedreven rol-mapping). Past de Beheerder de rechten aan, dan werkt dat door
/// bij de eerstvolgende login/refresh van de gebruiker.
/// </summary>
public static class AutorisatieBeleid
{
    public static void VoegCapabilityPoliciesToe(AuthorizationOptions options)
    {
        foreach (CapabilityDefinitie def in Capabilities.Alle)
        {
            options.AddPolicy(def.Sleutel, beleid =>
                beleid.RequireClaim(KinderKompasClaims.Capability, def.Sleutel));
        }
    }
}
