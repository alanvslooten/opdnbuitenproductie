using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Wachtlijst;

/// <summary>
/// Bewijst dat <see cref="Plaatsing.EersteVrijeDag"/> het eerste moment vindt
/// waarop de groepsbezetting op een weekdag onder het maximum zakt — typisch
/// doordat een geplaatst kind uitstroomt (einddatum/4e verjaardag).
/// </summary>
public class PlaatsingTests
{
    private static Kind Kind(DateOnly? einddatum)
        => new()
        {
            Voornaam = "K", Achternaam = "L",
            Geboortedatum = new DateOnly(2024, 6, 1),
            Startdatum = new DateOnly(2025, 1, 1),
            Einddatum = einddatum,
            Contracttype = Contracttype.Weken49,
            GewensteOpvangdagen = Weekdag.Maandag,
        };

    [Fact]
    public void EersteVrijeDag_GroepVolTotUitstroom_GeeftEersteMaandagNaUitstroom()
    {
        // Max 2; twee kinderen op maandag, één stroomt eind februari uit.
        var kinderen = new[] { Kind(einddatum: null), Kind(einddatum: new DateOnly(2026, 2, 28)) };

        DateOnly? vrij = Plaatsing.EersteVrijeDag(
            kinderen, Array.Empty<Schoolvakantie>(),
            Weekdag.Maandag, vanaf: new DateOnly(2026, 1, 5), maxKinderen: 2);

        // Eerste maandag waarop de vertrekker er niet meer is.
        Assert.Equal(new DateOnly(2026, 3, 2), vrij);
    }

    [Fact]
    public void EersteVrijeDag_DirectPlek_GeeftEersteVoorkomenVanDeWeekdag()
    {
        var kinderen = new[] { Kind(einddatum: null) }; // 1 kind, max 2 → altijd plek

        DateOnly? vrij = Plaatsing.EersteVrijeDag(
            kinderen, Array.Empty<Schoolvakantie>(),
            Weekdag.Dinsdag, vanaf: new DateOnly(2026, 1, 5), maxKinderen: 2);

        // 5 jan 2026 is een maandag; de eerstvolgende dinsdag is 6 jan.
        Assert.Equal(new DateOnly(2026, 1, 6), vrij);
    }

    [Fact]
    public void EersteVrijeDag_NooitPlekBinnenHorizon_GeeftNull()
    {
        var kinderen = new[] { Kind(einddatum: null), Kind(einddatum: null) }; // vol, max 2

        DateOnly? vrij = Plaatsing.EersteVrijeDag(
            kinderen, Array.Empty<Schoolvakantie>(),
            Weekdag.Maandag, vanaf: new DateOnly(2026, 1, 5), maxKinderen: 2,
            maxWekenVooruit: 8);

        Assert.Null(vrij);
    }

    [Fact]
    public void EersteVrijeDag_GeenEnkeleOpvangdag_Werpt()
    {
        Assert.Throws<ArgumentException>(() => Plaatsing.EersteVrijeDag(
            Array.Empty<Kind>(), Array.Empty<Schoolvakantie>(),
            Weekdag.Maandag | Weekdag.Dinsdag, new DateOnly(2026, 1, 5), maxKinderen: 2));
    }
}
