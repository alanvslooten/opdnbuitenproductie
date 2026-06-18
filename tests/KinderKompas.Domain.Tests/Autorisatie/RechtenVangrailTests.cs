using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Tests.Autorisatie;

/// <summary>
/// Bewijst de anti-uitsluiting-vangrail (fase 9c): de Beheerder kan z'n
/// instellingenbeheer en dashboardtoegang niet weggeven, terwijl andere rollen vrij
/// instelbaar blijven.
/// </summary>
public class RechtenVangrailTests
{
    [Fact]
    public void Beheerder_zonder_instellingenrecht_wordt_geblokkeerd()
    {
        var gevraagd = new[] { Capabilities.MagKinderenBeheren, Capabilities.MagDashboardZien };

        IReadOnlyList<string> ontbrekend =
            RechtenVangrail.OntbrekendeBeschermdeRechten(Rol.Beheerder, gevraagd);

        Assert.Contains(Capabilities.MagInstellingenBeheren, ontbrekend);
    }

    [Fact]
    public void Beheerder_met_alle_beschermde_rechten_mag_de_rest_vrij_kiezen()
    {
        var gevraagd = new[] { Capabilities.MagInstellingenBeheren, Capabilities.MagDashboardZien };

        IReadOnlyList<string> ontbrekend =
            RechtenVangrail.OntbrekendeBeschermdeRechten(Rol.Beheerder, gevraagd);

        Assert.Empty(ontbrekend);
    }

    [Theory]
    [InlineData(Rol.Hulpbeheerder)]
    [InlineData(Rol.Senior)]
    [InlineData(Rol.Junior)]
    [InlineData(Rol.Groepsportaal)]
    public void Andere_rollen_kennen_geen_vangrail(Rol rol)
    {
        // Zelfs een lege rechtenset is voor niet-Beheerders toegestaan.
        IReadOnlyList<string> ontbrekend =
            RechtenVangrail.OntbrekendeBeschermdeRechten(rol, Array.Empty<string>());

        Assert.Empty(ontbrekend);
    }
}
