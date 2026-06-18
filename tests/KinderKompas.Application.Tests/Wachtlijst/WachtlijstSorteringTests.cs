using KinderKompas.Application.Wachtlijst;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Wachtlijst;

/// <summary>
/// Bewijst de wachtlijst-volgorde: handmatig bovenaan (personeelskinderen) eerst,
/// daarna aflopende prioriteitsscore, en bij gelijke score de langst wachtende eerst.
/// </summary>
public class WachtlijstSorteringTests
{
    private static WachtlijstInschrijvingDto Dto(
        string naam, int score, bool bovenaan = false, DateOnly? inschrijf = null) =>
        new(
            Guid.NewGuid(), naam, naam,
            Geboortedatum: new DateOnly(2025, 1, 1),
            InschrijfdatumWachtlijst: inschrijf ?? new DateOnly(2026, 1, 1),
            GewensteStartdatum: new DateOnly(2026, 6, 1),
            GewensteOpvangdagen: Weekdag.Maandag,
            OpenstaandeDagen: Weekdag.Maandag,
            ReedsGeplaatsteDagen: Weekdag.Geen,
            Contracttype: Contracttype.Weken49,
            GewensteStamgroepId: null,
            IsIntern: false,
            HandmatigBovenaan: bovenaan,
            Status: WachtlijstStatus.Wachtend,
            Notitie: null,
            Prioriteitsscore: score,
            PrioriteitOnderdelen: Array.Empty<string>(),
            WordtBinnenkortVier: false,
            Oudercontact: null);

    [Fact]
    public void OpPrioriteit_ZetBovenaanEerst_DanScore_DanLangstWachtend()
    {
        var laag = Dto("Laag", score: 10);
        var hoog = Dto("Hoog", score: 500);
        var personeel = Dto("Personeel", score: 0, bovenaan: true);
        var hoogOuder = Dto("HoogOuder", score: 500, inschrijf: new DateOnly(2025, 1, 1));

        var gesorteerd = WachtlijstSortering.OpPrioriteit(new[] { laag, hoog, personeel, hoogOuder });

        // Personeelskind bovenaan, ondanks score 0.
        Assert.Equal("Personeel", gesorteerd[0].Voornaam);
        // Daarna gelijke score 500: de langst wachtende (2025) vóór de andere (2026).
        Assert.Equal("HoogOuder", gesorteerd[1].Voornaam);
        Assert.Equal("Hoog", gesorteerd[2].Voornaam);
        Assert.Equal("Laag", gesorteerd[3].Voornaam);
    }
}
