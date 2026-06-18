using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Wachtlijst;

/// <summary>
/// Bewijst dat de instelbare prioriteitsgewichten (fase 9c) de score daadwerkelijk
/// sturen: dezelfde regels, maar andere puntenwaarden geven een andere uitkomst, en de
/// standaardgewichten gedragen zich identiek aan de oude, vaste constanten.
/// </summary>
public class WachtlijstPrioriteitGewichtenTests
{
    private static WachtlijstInschrijving Inschrijving(DateOnly inschrijfdatum, bool intern) => new()
    {
        Voornaam = "K", Achternaam = "L",
        Geboortedatum = new DateOnly(2025, 1, 1),
        InschrijfdatumWachtlijst = inschrijfdatum,
        GewensteStartdatum = new DateOnly(2026, 1, 1),
        GewensteOpvangdagen = Weekdag.Maandag,
        IsIntern = intern,
    };

    [Fact]
    public void Aangepaste_gewichten_veranderen_de_score()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 1, 1), intern: true);
        DateOnly peil = new(2026, 7, 1); // 6 volledige maanden

        var gewichten = new WachtlijstPrioriteitsgewichten(PuntenIntern: 1000, PuntenPerMaandWachtend: 25);
        var uitkomst = WachtlijstPrioriteit.Bereken(inschrijving, peil, gewichten);

        // 1000 (intern) + 6 × 25 (anciënniteit) = 1150.
        Assert.Equal(1000 + 6 * 25, uitkomst.Score);
    }

    [Fact]
    public void Standaardgewichten_gedragen_zich_als_de_oude_constanten()
    {
        var inschrijving = Inschrijving(new DateOnly(2026, 1, 1), intern: true);
        DateOnly peil = new(2026, 7, 1);

        var metDefault = WachtlijstPrioriteit.Bereken(inschrijving, peil);
        var metExpliciet = WachtlijstPrioriteit.Bereken(inschrijving, peil, WachtlijstPrioriteitsgewichten.Standaard);

        int verwacht = WachtlijstPrioriteit.PuntenIntern + 6 * WachtlijstPrioriteit.PuntenPerMaandWachtend;
        Assert.Equal(verwacht, metDefault.Score);
        Assert.Equal(verwacht, metExpliciet.Score);
    }
}
