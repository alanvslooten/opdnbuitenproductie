using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Bkr;

/// <summary>
/// Tabel 1 — grenswaarden, zowel voor stamgroepen met één leeftijd als voor
/// gemengde leeftijdsgroepen. Bron: Tabel 1, Bijlage 1 Besluit kwaliteit kinderopvang.
/// </summary>
public class BkrCalculatorTabel1Tests
{
    // === Tabel 1 — één leeftijdscategorie: grenswaarden 1pm/2pm per categorie ===

    [Theory]
    // 0-1 jaar: 1 pm t/m 3, 2 pm t/m 6
    [InlineData(3, 0, 0, 0, 1)]
    [InlineData(4, 0, 0, 0, 2)]
    [InlineData(6, 0, 0, 0, 2)]
    [InlineData(7, 0, 0, 0, 3)]
    // 1-2 jaar: 1 pm t/m 5, 2 pm t/m 10
    [InlineData(0, 5, 0, 0, 1)]
    [InlineData(0, 6, 0, 0, 2)]
    [InlineData(0, 10, 0, 0, 2)]
    [InlineData(0, 11, 0, 0, 3)]
    // 2-3 jaar: 1 pm t/m 8, 2 pm t/m 16
    [InlineData(0, 0, 8, 0, 1)]
    [InlineData(0, 0, 9, 0, 2)]
    [InlineData(0, 0, 16, 0, 2)]
    // 3-4 jaar: 1 pm t/m 8, 2 pm t/m 16
    [InlineData(0, 0, 0, 8, 1)]
    [InlineData(0, 0, 0, 9, 2)]
    [InlineData(0, 0, 0, 16, 2)]
    public void EnkeleLeeftijd_geeft_juiste_grenswaarden(
        int nul, int een, int twee, int drie, int verwachtPmers)
    {
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(nul, een, twee, drie));

        Assert.Equal(verwachtPmers, uitkomst.VereisteHoeveelheidPmers);
    }

    // === Tabel 1 — gemengde leeftijdsgroepen: alle rijen, 1pm- en 2pm-maxima ===

    [Theory]
    // 0-2 jaar (0-1 + 1-2): max 4 (1pm), 8 (2pm)
    [InlineData(1, 3, 0, 0, 1)]
    [InlineData(2, 3, 0, 0, 2)]
    [InlineData(4, 4, 0, 0, 2)]
    // 0-3 jaar (0-1 .. 2-3): max 5 (1pm), 10 (2pm)
    [InlineData(1, 1, 3, 0, 1)]
    [InlineData(2, 2, 2, 0, 2)]
    [InlineData(3, 3, 4, 0, 2)]
    // 0-4 jaar (0-1 .. 3-4): max 5 (1pm), 12 (2pm)
    [InlineData(1, 1, 1, 2, 1)]
    [InlineData(2, 2, 2, 6, 2)]
    // 1-3 jaar (1-2 + 2-3): max 6 (1pm), 11 (2pm)
    [InlineData(0, 3, 3, 0, 1)]
    [InlineData(0, 5, 6, 0, 2)]
    // 1-4 jaar (1-2 .. 3-4): max 7 (1pm), 13 (2pm)
    [InlineData(0, 3, 2, 2, 1)]
    [InlineData(0, 5, 4, 4, 2)]
    // 2-4 jaar (2-3 + 3-4): max 8 (1pm), 16 (2pm)
    [InlineData(0, 0, 5, 3, 1)]
    [InlineData(0, 0, 8, 8, 2)]
    public void GemengdeGroep_geeft_juiste_grenswaarden(
        int nul, int een, int twee, int drie, int verwachtPmers)
    {
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(nul, een, twee, drie));

        Assert.True(uitkomst.VereisteHoeveelheidPmers >= verwachtPmers,
            $"Verwacht minimaal {verwachtPmers} pm'er(s), kreeg {uitkomst.VereisteHoeveelheidPmers}.");
        // Voor deze gevallen (geen baby-overschot) is Tabel 1 leidend en exact.
        Assert.Equal(verwachtPmers, uitkomst.UitkomstTabel1);
    }

    [Fact]
    public void LegeGroep_vereist_nul_pmers()
    {
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(0, 0, 0, 0));

        Assert.Equal(0, uitkomst.VereisteHoeveelheidPmers);
        Assert.Equal(BkrStap.Tabel1, uitkomst.LeidendeStap);
    }
}
