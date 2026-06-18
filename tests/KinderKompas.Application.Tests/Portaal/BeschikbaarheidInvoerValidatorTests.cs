using KinderKompas.Application.Portaal;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Portaal;

/// <summary>
/// De medewerker geeft in het thuis-portaal z'n eigen beschikbaarheid op; die mag
/// alleen geldige opvangdagen (ma-vr) bevatten. De overlapcontrole met de vaste
/// werkdagen leeft in de controller (die de vaste dagen kent) en valt buiten deze validator.
/// </summary>
public class BeschikbaarheidInvoerValidatorTests
{
    private readonly BeschikbaarheidInvoerValidator _validator = new();

    [Fact]
    public void GeldigeWeekdagen_ZijnToegestaan()
    {
        var invoer = new BeschikbaarheidInvoer(Weekdag.Donderdag | Weekdag.Vrijdag);

        Assert.True(_validator.Validate(invoer).IsValid);
    }

    [Fact]
    public void Geen_IsToegestaan()
    {
        var invoer = new BeschikbaarheidInvoer(Weekdag.Geen);

        Assert.True(_validator.Validate(invoer).IsValid);
    }

    [Fact]
    public void OngeldigeDagvlag_WordtAfgekeurd()
    {
        // Een vlag buiten ma-vr (bit voor 'zaterdag' bestaat niet in het domein).
        var invoer = new BeschikbaarheidInvoer((Weekdag)32);

        Assert.False(_validator.Validate(invoer).IsValid);
    }
}
