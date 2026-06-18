using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Tests.Wachtlijst;

/// <summary>
/// Bewijst de kern van het deelvoorstel: een geaccepteerd voorstel voor een subset
/// van de gewenste dagen plaatst alléén die dagen; de resterende gewenste dagen
/// blijven open (en dus op de wachtlijst). Pas als alle dagen gedekt zijn, is de
/// inschrijving volledig geplaatst.
/// </summary>
public class WachtlijstInschrijvingTests
{
    private static WachtlijstInschrijving Inschrijving(Weekdag gewenst) => new()
    {
        Voornaam = "K", Achternaam = "L",
        Geboortedatum = new DateOnly(2025, 1, 1),
        InschrijfdatumWachtlijst = new DateOnly(2026, 1, 1),
        GewensteStartdatum = new DateOnly(2026, 3, 1),
        GewensteOpvangdagen = gewenst,
    };

    [Fact]
    public void OpenstaandeDagen_ZonderVoorstel_IsAlleGewensteDagen()
    {
        var inschrijving = Inschrijving(Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag);

        Assert.Equal(
            Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag,
            inschrijving.OpenstaandeDagen);
        Assert.False(inschrijving.IsVolledigGeplaatst);
    }

    [Fact]
    public void VerwerkGeaccepteerdVoorstel_Deelvoorstel_LaatRestOpDeWachtlijst()
    {
        var inschrijving = Inschrijving(Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag);

        // Deelvoorstel voor ma + di; woensdag blijft gewenst maar ongeplaatst.
        inschrijving.VerwerkGeaccepteerdVoorstel(Weekdag.Maandag | Weekdag.Dinsdag);

        Assert.Equal(Weekdag.Woensdag, inschrijving.OpenstaandeDagen);
        Assert.False(inschrijving.IsVolledigGeplaatst);
        Assert.Equal(WachtlijstStatus.Wachtend, inschrijving.Status);
    }

    [Fact]
    public void VerwerkGeaccepteerdVoorstel_AlleDagen_MaaktVolledigGeplaatst()
    {
        var inschrijving = Inschrijving(Weekdag.Maandag | Weekdag.Dinsdag);

        inschrijving.VerwerkGeaccepteerdVoorstel(Weekdag.Maandag);
        Assert.Equal(WachtlijstStatus.Wachtend, inschrijving.Status);

        inschrijving.VerwerkGeaccepteerdVoorstel(Weekdag.Dinsdag);

        Assert.Equal(Weekdag.Geen, inschrijving.OpenstaandeDagen);
        Assert.True(inschrijving.IsVolledigGeplaatst);
        Assert.Equal(WachtlijstStatus.Geplaatst, inschrijving.Status);
    }

    [Fact]
    public void VerwerkGeaccepteerdVoorstel_NegeertNietGewensteDagen()
    {
        var inschrijving = Inschrijving(Weekdag.Maandag);

        // Voorstel bevat ook donderdag, die niet gewenst is: telt niet mee.
        inschrijving.VerwerkGeaccepteerdVoorstel(Weekdag.Maandag | Weekdag.Donderdag);

        Assert.Equal(Weekdag.Maandag, inschrijving.ReedsGeplaatsteDagen);
        Assert.True(inschrijving.IsVolledigGeplaatst);
    }
}
