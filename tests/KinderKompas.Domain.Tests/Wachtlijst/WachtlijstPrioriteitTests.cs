using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Wachtlijst;

/// <summary>
/// Bewijst dat de prioriteitsscore deterministisch en transparant is: interne
/// aanvragen krijgen de vaste bonus, anciënniteit telt per volledige maand, en
/// het handmatig-bovenaan-signaal staat los van de score.
/// </summary>
public class WachtlijstPrioriteitTests
{
    private static WachtlijstInschrijving Inschrijving(
        DateOnly inschrijfdatum, bool intern = false, bool bovenaan = false) => new()
    {
        Voornaam = "K", Achternaam = "L",
        Geboortedatum = new DateOnly(2025, 1, 1),
        InschrijfdatumWachtlijst = inschrijfdatum,
        GewensteStartdatum = new DateOnly(2026, 1, 1),
        GewensteOpvangdagen = Weekdag.Maandag,
        IsIntern = intern,
        HandmatigBovenaan = bovenaan,
    };

    [Fact]
    public void Bereken_ExterneAanvraag_TeltAlleenAncienniteit()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 1, 1));

        // Precies 6 volledige maanden later.
        var uitkomst = WachtlijstPrioriteit.Bereken(inschrijving, new DateOnly(2026, 7, 1));

        Assert.Equal(6 * WachtlijstPrioriteit.PuntenPerMaandWachtend, uitkomst.Score);
        Assert.False(uitkomst.HandmatigBovenaan);
    }

    [Fact]
    public void Bereken_InterneAanvraag_KrijgtBonusBovenopAncienniteit()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 1, 1), intern: true);

        var uitkomst = WachtlijstPrioriteit.Bereken(inschrijving, new DateOnly(2026, 4, 1));

        int verwacht = WachtlijstPrioriteit.PuntenIntern + 3 * WachtlijstPrioriteit.PuntenPerMaandWachtend;
        Assert.Equal(verwacht, uitkomst.Score);
    }

    [Fact]
    public void Bereken_NogGeenVolleMaand_GeeftGeenAncienniteit()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 1, 20));

        // Minder dan een volledige maand later (dag valt vóór de inschrijfdag).
        var uitkomst = WachtlijstPrioriteit.Bereken(inschrijving, new DateOnly(2026, 2, 10));

        Assert.Equal(0, uitkomst.Score);
    }

    [Fact]
    public void Bereken_ToekomstigeInschrijfdatum_GeeftGeenNegatieveScore()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 12, 1));

        var uitkomst = WachtlijstPrioriteit.Bereken(inschrijving, new DateOnly(2026, 6, 1));

        Assert.Equal(0, uitkomst.Score);
    }

    [Fact]
    public void Bereken_HandmatigBovenaan_ZetVlagMaarVerandertScoreNiet()
    {
        var gewoon = Inschrijving(new DateOnly(2026, 1, 1));
        var bovenaan = Inschrijving(new DateOnly(2026, 1, 1), bovenaan: true);
        var peil = new DateOnly(2026, 5, 1);

        var u1 = WachtlijstPrioriteit.Bereken(gewoon, peil);
        var u2 = WachtlijstPrioriteit.Bereken(bovenaan, peil);

        Assert.Equal(u1.Score, u2.Score);
        Assert.False(u1.HandmatigBovenaan);
        Assert.True(u2.HandmatigBovenaan);
    }
}
