using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Bkr;

/// <summary>
/// Formule Z (baby-correctie) en de MAX-regel. Bron: Bijlage 1, formule Z =
/// A + ((B + C + D) / 1,2), altijd naar boven afgerond; einduitkomst = MAX(Tabel 1, Z).
/// </summary>
public class BkrCalculatorFormuleZTests
{
    /// <summary>
    /// Het uitgewerkte rekenvoorbeeld uit het brondocument:
    /// 2 baby's (0-1), 3 peuters (1-2), 4 dreumesen (2-3), 2 kleuters (3-4).
    /// Z ≈ 1,93 → 2 ; Tabel 1 (gemengd 0-4, 11 kinderen) → 2 ; einduitkomst 2.
    /// </summary>
    [Fact]
    public void Rekenvoorbeeld_uit_brondocument_geeft_2_pmers()
    {
        var groep = new GroepSamenstelling(2, 3, 4, 2);

        var uitkomst = BkrCalculator.Bereken(groep);

        Assert.Equal(2, uitkomst.VereisteHoeveelheidPmers);
        Assert.Equal(2, uitkomst.UitkomstTabel1);
        Assert.Equal(2, uitkomst.UitkomstFormuleZ);
        // Z moet rond 1,93 liggen (vóór afronding).
        Assert.InRange(uitkomst.FormuleZ, 1.90m, 1.94m);
        // Bij gelijke uitkomst is Tabel 1 (stap 1) de leidende basis.
        Assert.Equal(BkrStap.Tabel1, uitkomst.LeidendeStap);
    }

    [Fact]
    public void FormuleZ_wordt_altijd_naar_boven_afgerond()
    {
        // 1 baby alleen: Z = 1/3 = 0,333 → naar boven = 1 (niet 0).
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(1, 0, 0, 0));

        Assert.True(uitkomst.FormuleZ < 1m, "Ruwe Z moet kleiner dan 1 zijn.");
        Assert.Equal(1, uitkomst.UitkomstFormuleZ);
        Assert.Equal(1, uitkomst.VereisteHoeveelheidPmers);
    }

    [Fact]
    public void FormuleZ_is_leidend_wanneer_hoger_dan_Tabel1()
    {
        // 6 baby's + 1 kleuter: gemengde groep 0-4, 7 kinderen.
        // Tabel 1: 7 ≤ 12 (2pm-max) → 2 pm'ers.
        // Z: A = 6/3 = 2 ; D = 1/8 = 0,125 ; (0,125/1,2) ≈ 0,104 ; Z ≈ 2,10 → 3 pm'ers.
        // Einduitkomst = MAX(2, 3) = 3, leidend: Formule Z.
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(6, 0, 0, 1));

        Assert.Equal(2, uitkomst.UitkomstTabel1);
        Assert.Equal(3, uitkomst.UitkomstFormuleZ);
        Assert.Equal(3, uitkomst.VereisteHoeveelheidPmers);
        Assert.Equal(BkrStap.FormuleZ, uitkomst.LeidendeStap);
    }

    [Fact]
    public void Einduitkomst_is_altijd_het_maximum_van_beide_stappen()
    {
        // Willekeurige baby-houdende groep: einduitkomst moet >= beide stappen zijn,
        // en gelijk aan de hoogste.
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(3, 2, 0, 0));

        int verwacht = Math.Max(uitkomst.UitkomstTabel1, uitkomst.UitkomstFormuleZ);
        Assert.Equal(verwacht, uitkomst.VereisteHoeveelheidPmers);
    }

    [Fact]
    public void ZonderBabys_is_FormuleZ_nooit_leidend()
    {
        // Zonder baby's deelt formule Z elke categorie nog eens door 1,2 en blijft
        // daardoor altijd onder of gelijk aan Tabel 1.
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(0, 5, 4, 4));

        Assert.Equal(BkrStap.Tabel1, uitkomst.LeidendeStap);
        Assert.True(uitkomst.UitkomstFormuleZ <= uitkomst.UitkomstTabel1);
    }

    [Fact]
    public void Uitkomst_bevat_leesbare_stap_voor_stap_uitleg()
    {
        var uitkomst = BkrCalculator.Bereken(new GroepSamenstelling(2, 3, 4, 2));

        Assert.NotEmpty(uitkomst.Stappen);
        Assert.Contains(uitkomst.Stappen, s => s.Contains("Stap 1"));
        Assert.Contains(uitkomst.Stappen, s => s.Contains("Einduitkomst"));
    }
}
