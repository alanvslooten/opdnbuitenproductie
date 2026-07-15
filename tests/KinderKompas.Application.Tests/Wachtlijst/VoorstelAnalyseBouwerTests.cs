using KinderKompas.Application.Wachtlijst;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Tests.Wachtlijst;

/// <summary>
/// Bewijst dat de voorstel-analyse de BKR-impact (huidig én mét de kandidaat erbij)
/// rechtstreeks uit de Domain-calculator haalt, dat alleen de openstaande dagen
/// worden geanalyseerd, en dat "wanneer komt er een plek vrij?" klopt.
/// </summary>
public class VoorstelAnalyseBouwerTests
{
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    // 7 sep 2026 is een maandag.
    private static readonly DateOnly Startmaandag = new(2026, 9, 7);
    private static readonly DateOnly BabyGeboorte = new(2026, 3, 1); // ~0,5 jaar op de peilweek

    private static Kind Baby(Guid groepId, DateOnly? einddatum = null) => new()
    {
        Voornaam = "B", Achternaam = "B",
        Geboortedatum = BabyGeboorte,
        StamgroepId = groepId,
        Startdatum = new DateOnly(2026, 6, 1),
        Einddatum = einddatum,
        Contracttype = Contracttype.Weken49,
        GewensteOpvangdagen = AlleWeekdagen,
    };

    private static WachtlijstInschrijving Kandidaat(Weekdag gewenst, Weekdag reedsGeplaatst = Weekdag.Geen) => new()
    {
        Voornaam = "Nieuw", Achternaam = "Kind",
        Geboortedatum = BabyGeboorte,
        InschrijfdatumWachtlijst = new DateOnly(2026, 1, 1),
        GewensteStartdatum = Startmaandag,
        GewensteOpvangdagen = gewenst,
        ReedsGeplaatsteDagen = reedsGeplaatst,
        Contracttype = Contracttype.Weken49,
    };

    [Fact]
    public void Bouw_BkrImpact_KomtExactUitDeDomeinCalculator()
    {
        Guid groepId = Guid.NewGuid();
        var groep = new Stamgroep { Id = groepId, Naam = "Bengeltjes", MaxKinderen = 12 };
        groep.Kinderen = new List<Kind> { Baby(groepId), Baby(groepId), Baby(groepId) };

        var analyse = VoorstelAnalyseBouwer.Bouw(
            Kandidaat(Weekdag.Maandag), groep, Array.Empty<Schoolvakantie>());

        VoorstelDagAnalyseDto maandag = Assert.Single(analyse.Dagen);

        // Onafhankelijk dezelfde berekening via de domeincalculator.
        var nu = new GroepSamenstelling(3, 0, 0, 0);
        BkrUitkomst verwachtNu = BkrCalculator.Bereken(nu);
        BkrUitkomst verwachtNa = BkrCalculator.Bereken(nu.MetExtra(Leeftijdsgroep.NulTotEen));

        Assert.Equal(3, maandag.AantalAanwezigNu);
        Assert.Equal(verwachtNu.VereisteHoeveelheidPmers, maandag.VereistePmersNu);
        Assert.Equal(4, maandag.AantalAanwezigNa);
        Assert.Equal(verwachtNa.VereisteHoeveelheidPmers, maandag.VereistePmersNa);
        Assert.True(maandag.ExtraPmerNodig); // 1 → 2 pm'ers
        Assert.False(maandag.BkrOverschrijdtNa);
    }

    [Fact]
    public void Bouw_AnalyseertAlleenOpenstaandeDagen()
    {
        Guid groepId = Guid.NewGuid();
        var groep = new Stamgroep { Id = groepId, Naam = "Boefjes", MaxKinderen = 12 };

        // Gewenst ma+di+wo, maar di is al geplaatst → alleen ma en wo open.
        var kandidaat = Kandidaat(
            Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag,
            reedsGeplaatst: Weekdag.Dinsdag);

        var analyse = VoorstelAnalyseBouwer.Bouw(kandidaat, groep, Array.Empty<Schoolvakantie>());

        Assert.Equal(Weekdag.Maandag | Weekdag.Woensdag, analyse.OpenstaandeDagen);
        Assert.Equal(2, analyse.Dagen.Count);
        Assert.DoesNotContain(analyse.Dagen, d => d.Weekdag == Weekdag.Dinsdag);
    }

    [Fact]
    public void Bouw_GroepVol_GeeftGeenPlekEnEersteVrijeDatumNaUitstroom()
    {
        Guid groepId = Guid.NewGuid();
        var groep = new Stamgroep { Id = groepId, Naam = "Klein", MaxKinderen = 3 };
        // 3 baby's op maandag → vol; één stroomt eind september uit.
        groep.Kinderen = new List<Kind>
        {
            Baby(groepId), Baby(groepId), Baby(groepId, einddatum: new DateOnly(2026, 9, 30)),
        };

        var analyse = VoorstelAnalyseBouwer.Bouw(
            Kandidaat(Weekdag.Maandag), groep, Array.Empty<Schoolvakantie>());

        VoorstelDagAnalyseDto maandag = Assert.Single(analyse.Dagen);
        Assert.False(maandag.PlekVrijOpStart);
        Assert.False(analyse.GroepBlijftOnderMax); // 3/3 geplaatst
        // Eerste maandag waarop de vertrekker weg is: 5 okt 2026.
        Assert.Equal(new DateOnly(2026, 10, 5), maandag.EersteVrijeDatum);
    }

    [Fact]
    public void Bouw_KandidaatTeOud_MarkeertBuitenOpvangleeftijd()
    {
        Guid groepId = Guid.NewGuid();
        var groep = new Stamgroep { Id = groepId, Naam = "Bengeltjes", MaxKinderen = 12 };

        var kandidaat = Kandidaat(Weekdag.Maandag);
        kandidaat.Geboortedatum = new DateOnly(2020, 1, 1); // ruim 4 jaar op de startdatum

        var analyse = VoorstelAnalyseBouwer.Bouw(kandidaat, groep, Array.Empty<Schoolvakantie>());

        Assert.True(analyse.KandidaatBuitenOpvangleeftijd);
        Assert.Null(analyse.KandidaatLeeftijdsgroep);
        Assert.Null(Assert.Single(analyse.Dagen).VereistePmersNa);
    }

    [Fact]
    public void Bouw_TeltOpenstaandeVoorstellenMeeInDeBezettingEnBkr()
    {
        Guid groepId = Guid.NewGuid();
        var groep = new Stamgroep { Id = groepId, Naam = "Bengeltjes", MaxKinderen = 12 };
        // Drie baby's al geplaatst. Twee openstaande voorstellen (elk een baby op maandag)
        // moeten als voorlopige bezetting meetellen, zodat de BKR-baseline voller is.
        groep.Kinderen = new List<Kind> { Baby(groepId), Baby(groepId), Baby(groepId) };
        var openVoorstelKinderen = new List<Kind> { Baby(groepId), Baby(groepId) };

        var zonder = VoorstelAnalyseBouwer.Bouw(
            Kandidaat(Weekdag.Maandag), groep, Array.Empty<Schoolvakantie>());
        var met = VoorstelAnalyseBouwer.Bouw(
            Kandidaat(Weekdag.Maandag), groep, Array.Empty<Schoolvakantie>(),
            peilStartdatum: null, openVoorstelKinderen: openVoorstelKinderen);

        VoorstelDagAnalyseDto zonderMa = Assert.Single(zonder.Dagen);
        VoorstelDagAnalyseDto metMa = Assert.Single(met.Dagen);

        // Zonder meetellen: 3 aanwezig. Mét: 3 + 2 voorlopige = 5 aanwezig op maandag.
        Assert.Equal(3, zonderMa.AantalAanwezigNu);
        Assert.Equal(5, metMa.AantalAanwezigNu);
        Assert.Equal(0, zonder.OpenVoorstellenMeegeteld);
        Assert.Equal(2, met.OpenVoorstellenMeegeteld);
        // De projectie ná plaatsing telt door op de vollere baseline: 5 + kandidaat = 6.
        Assert.Equal(6, metMa.AantalAanwezigNa);
    }
}
