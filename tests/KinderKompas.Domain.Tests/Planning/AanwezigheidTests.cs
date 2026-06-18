using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Planning;

/// <summary>
/// Bewijst de plannings-kernregels (fase 4): wie is er op een dag, en hoe verhoudt
/// de geplande aanwezigheid zich tot de contractvorm, de opvangdagen, de
/// opvangleeftijd en de schoolvakanties. De BKR per dag komt aantoonbaar uit de
/// bestaande <see cref="BkrCalculator"/> op basis van de aanwezige kinderen.
/// </summary>
public class AanwezigheidTests
{
    // Alle dagen ma t/m vr als gewenste opvangdagen, zodat de weekdag in de
    // testopzet nooit zelf de blokkerende factor is.
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    // Woensdag, midden in de zomervakantie hieronder.
    private static readonly DateOnly VakantieWoensdag = new(2026, 8, 5);

    private static Schoolvakantie Zomervakantie2026 => new()
    {
        Naam = "Zomervakantie",
        Schooljaar = 2025,
        Begindatum = new DateOnly(2026, 7, 13),
        Einddatum = new DateOnly(2026, 8, 23),
    };

    private static Kind MaakKind(
        DateOnly geboortedatum,
        Contracttype contracttype,
        DateOnly? startdatum = null,
        DateOnly? einddatum = null,
        Weekdag opvangdagen = AlleWeekdagen) => new()
    {
        Voornaam = "Test",
        Achternaam = "Kind",
        Geboortedatum = geboortedatum,
        Contracttype = contracttype,
        Startdatum = startdatum ?? new DateOnly(2025, 1, 1),
        Einddatum = einddatum,
        GewensteOpvangdagen = opvangdagen,
        StamgroepId = Guid.NewGuid(),
    };

    [Fact]
    public void VakantieWoensdag_IsEenWeekdag() // borgt de testopzet
        => Assert.NotEqual(Weekdag.Geen, Aanwezigheid.NaarWeekdag(VakantieWoensdag));

    [Fact]
    public void VeertigWekenkind_TeltNietMeeInSchoolvakantie_EnBkrKomtUitDeCalculator()
    {
        // Arrange: vijf 49-wekenkinderen en twee 40-wekenkinderen, allen ~2 jaar
        // (categorie 2-3) op de peildatum en allen op woensdag aanwezig.
        DateOnly tweeJaarOud = new(2024, 1, 1); // op 2026-08-05 precies 2 jaar
        var kinderen = new List<Kind>
        {
            MaakKind(tweeJaarOud, Contracttype.Weken49),
            MaakKind(tweeJaarOud, Contracttype.Weken49),
            MaakKind(tweeJaarOud, Contracttype.Weken49),
            MaakKind(tweeJaarOud, Contracttype.Weken49),
            MaakKind(tweeJaarOud, Contracttype.Weken49),
            MaakKind(tweeJaarOud, Contracttype.Weken40),
            MaakKind(tweeJaarOud, Contracttype.Weken40),
        };
        var vakanties = new[] { Zomervakantie2026 };

        // Act
        IReadOnlyList<Kind> aanwezig =
            Aanwezigheid.AanwezigOp(kinderen, VakantieWoensdag, vakanties);
        GroepSamenstelling samenstelling =
            Aanwezigheid.SamenstellingOp(kinderen, VakantieWoensdag, vakanties);
        BkrUitkomst bkr = BkrCalculator.Bereken(samenstelling);

        // Assert: de twee 40-wekenkinderen tellen in de vakantieweek niet mee.
        Assert.Equal(5, aanwezig.Count);
        Assert.All(aanwezig, k => Assert.Equal(Contracttype.Weken49, k.Contracttype));

        // De samenstelling bevat exact de vijf aanwezige 2-3 jarigen.
        Assert.Equal(5, samenstelling.AantalTweeTotDrie);
        Assert.Equal(5, samenstelling.Totaal);

        // De BKR per dag is wat de Domain-calculator op die samenstelling geeft:
        // 5 kinderen van 2-3 jaar bij ratio 8 → 1 pm'er. (BkrUitkomst kent geen
        // value-equality op de Stappen-lijst, dus vergelijk de scalaire uitkomst.)
        BkrUitkomst verwacht = BkrCalculator.Bereken(new GroepSamenstelling(0, 0, 5, 0));
        Assert.Equal(verwacht.VereisteHoeveelheidPmers, bkr.VereisteHoeveelheidPmers);
        Assert.Equal(verwacht.UitkomstTabel1, bkr.UitkomstTabel1);
        Assert.Equal(verwacht.LeidendeStap, bkr.LeidendeStap);
        Assert.Equal(1, bkr.VereisteHoeveelheidPmers);
    }

    [Fact]
    public void VeertigWekenkind_IsBuitenVakantieGewoonAanwezig()
    {
        // Een gewone schoolweek-woensdag, ruim buiten elke vakantie.
        DateOnly schoolWoensdag = new(2026, 9, 9);
        var kind = MaakKind(new DateOnly(2024, 1, 1), Contracttype.Weken40);

        Assert.True(Aanwezigheid.IsKindAanwezigOp(kind, schoolWoensdag, new[] { Zomervakantie2026 }));
        Assert.False(Aanwezigheid.IsKindAanwezigOp(kind, VakantieWoensdag, new[] { Zomervakantie2026 }));
    }

    [Fact]
    public void NegenenveertigWekenkind_IsOokInVakantieAanwezig()
    {
        var kind = MaakKind(new DateOnly(2024, 1, 1), Contracttype.Weken49);
        Assert.True(Aanwezigheid.IsKindAanwezigOp(kind, VakantieWoensdag, new[] { Zomervakantie2026 }));
    }

    [Fact]
    public void Kind_IsAlleenAanwezigOpGewensteOpvangdagen()
    {
        // Alleen maandag gewenst; de woensdag valt af.
        var kind = MaakKind(new DateOnly(2024, 1, 1), Contracttype.Weken49, opvangdagen: Weekdag.Maandag);
        DateOnly maandag = new(2026, 9, 7);
        DateOnly woensdag = new(2026, 9, 9);

        Assert.True(Aanwezigheid.IsKindAanwezigOp(kind, maandag, Array.Empty<Schoolvakantie>()));
        Assert.False(Aanwezigheid.IsKindAanwezigOp(kind, woensdag, Array.Empty<Schoolvakantie>()));
    }

    [Fact]
    public void Kind_BuitenContractlooptijd_IsNietAanwezig()
    {
        var kind = MaakKind(
            new DateOnly(2024, 1, 1),
            Contracttype.Weken49,
            startdatum: new DateOnly(2026, 9, 1),
            einddatum: new DateOnly(2026, 12, 31));

        Assert.False(Aanwezigheid.IsKindAanwezigOp(kind, new DateOnly(2026, 8, 26), Array.Empty<Schoolvakantie>())); // vóór start
        Assert.True(Aanwezigheid.IsKindAanwezigOp(kind, new DateOnly(2026, 9, 2), Array.Empty<Schoolvakantie>()));   // binnen
        Assert.False(Aanwezigheid.IsKindAanwezigOp(kind, new DateOnly(2027, 1, 6), Array.Empty<Schoolvakantie>()));  // ná einde
    }

    [Fact]
    public void Kind_OpZijnVierdeVerjaardag_TeltNietMeer()
    {
        // Geen expliciete einddatum: de 4e verjaardag is de effectieve einddatum.
        var kind = MaakKind(new DateOnly(2022, 9, 9), Contracttype.Weken49); // wordt 4 op 2026-09-09 (woensdag)
        DateOnly dagVoor = new(2026, 9, 8);  // maandag? -> kies een opvangdag
        DateOnly verjaardag = new(2026, 9, 9);

        Assert.True(Aanwezigheid.IsKindAanwezigOp(kind, dagVoor, Array.Empty<Schoolvakantie>()));
        Assert.False(Aanwezigheid.IsKindAanwezigOp(kind, verjaardag, Array.Empty<Schoolvakantie>()));
    }

    [Fact]
    public void WordtBinnenkortVier_GeeftSignaalBinnenMarge()
    {
        var kind = MaakKind(new DateOnly(2022, 8, 1), Contracttype.Weken49); // 4 op 2026-08-01

        Assert.True(kind.WordtBinnenkortVier(new DateOnly(2026, 6, 1)));   // ~2 maanden ervoor
        Assert.False(kind.WordtBinnenkortVier(new DateOnly(2026, 1, 1)));  // ruim 7 maanden ervoor
        Assert.False(kind.WordtBinnenkortVier(new DateOnly(2026, 8, 1)));  // op de verjaardag zelf niet meer "bijna"
    }
}
