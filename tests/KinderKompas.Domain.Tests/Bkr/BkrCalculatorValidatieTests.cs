using KinderKompas.Domain.Exceptions;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Bkr;

/// <summary>
/// Validatie van wettelijke maxima en de voetnoot-sublimiet voor baby's, plus de
/// 3-uursregeling en rekenregel 2.
/// </summary>
public class BkrCalculatorValidatieTests
{
    [Theory]
    [InlineData(13, 0, 0, 0)]  // 0-1 jaar: max 12
    [InlineData(0, 17, 0, 0)]  // 1-2 jaar: max 16
    [InlineData(0, 0, 17, 0)]  // 2-3 jaar: max 16
    [InlineData(0, 0, 0, 17)]  // 3-4 jaar: max 16
    public void EnkeleLeeftijd_boven_maximum_gooit_exception(
        int nul, int een, int twee, int drie)
    {
        var groep = new GroepSamenstelling(nul, een, twee, drie);

        Assert.Throws<GroepOverschrijdtMaximumException>(() => BkrCalculator.Bereken(groep));
    }

    [Theory]
    [InlineData(5, 4, 0, 0)]   // 0-2 jaar: max 8 (2pm), hier 9
    [InlineData(2, 2, 7, 0)]   // 0-3 jaar: max 10 (2pm), hier 11
    [InlineData(2, 3, 4, 4)]   // 0-4 jaar: max 12 (2pm), hier 13
    [InlineData(0, 6, 6, 0)]   // 1-3 jaar: max 11 (2pm), hier 12
    [InlineData(0, 0, 9, 8)]   // 2-4 jaar: max 16 (2pm), hier 17
    public void GemengdeGroep_boven_maximum_gooit_exception(
        int nul, int een, int twee, int drie)
    {
        var groep = new GroepSamenstelling(nul, een, twee, drie);

        Assert.Throws<GroepOverschrijdtMaximumException>(() => BkrCalculator.Bereken(groep));
    }

    [Fact]
    public void Sublimiet_0tot3_groep_meer_dan_8_babys_gooit_exception()
    {
        // 0-3 groep (0-1 .. 2-3), totaal 10 (≤ 2pm-max), maar 9 baby's > sublimiet 8.
        var groep = new GroepSamenstelling(9, 0, 1, 0);

        Assert.Throws<GroepOverschrijdtMaximumException>(() => BkrCalculator.Bereken(groep));
    }

    // === 3-uursregeling ===

    [Fact]
    public void Driehursregeling_vereist_minimaal_1_pmer_bij_kinderen()
    {
        // Strikte BKR vraagt hier 2 pm'ers, maar binnen het afwijkvenster mag het naar 1.
        var groep = new GroepSamenstelling(2, 3, 4, 2);

        var strikt = BkrCalculator.Bereken(groep).VereisteHoeveelheidPmers;
        int afwijk = BkrCalculator.MinimaleBezettingDriehursregeling(groep);

        Assert.Equal(2, strikt);
        Assert.Equal(1, afwijk);
        Assert.True(afwijk < strikt, "Binnen de 3-uursregeling mag de bezetting lager dan de strikte BKR.");
    }

    [Fact]
    public void Driehursregeling_geeft_nul_bij_lege_groep()
    {
        var leeg = new GroepSamenstelling(0, 0, 0, 0);

        Assert.Equal(0, BkrCalculator.MinimaleBezettingDriehursregeling(leeg));
    }

    // === Rekenregel 2 ===

    [Fact]
    public void Rekenregel2_verhoogt_met_1_bij_afrondingsrandsituatie()
    {
        // Als toevoegen van een kind het vereiste zou doen dalen (van 2 naar 1),
        // wordt het met 1 verhoogd zodat het niet daalt.
        Assert.Equal(2, BkrCalculator.PasRekenregel2Toe(vereistZonderExtraKind: 2, vereistMetExtraKind: 1));
    }

    [Fact]
    public void Rekenregel2_laat_normale_stijging_ongemoeid()
    {
        Assert.Equal(3, BkrCalculator.PasRekenregel2Toe(vereistZonderExtraKind: 2, vereistMetExtraKind: 3));
        Assert.Equal(2, BkrCalculator.PasRekenregel2Toe(vereistZonderExtraKind: 2, vereistMetExtraKind: 2));
    }

    [Fact]
    public void Bereken_is_monotoon_een_kind_erbij_verlaagt_nooit_het_vereiste()
    {
        // Property: in de echte rekenkern mag het vereiste aantal nooit dalen door een
        // kind toe te voegen (de randsituatie die rekenregel 2 afvangt komt dan niet voor).
        var basis = new GroepSamenstelling(2, 2, 2, 2);
        int vereistBasis = BkrCalculator.Bereken(basis).VereisteHoeveelheidPmers;

        foreach (var metKind in new[]
        {
            new GroepSamenstelling(3, 2, 2, 2),
            new GroepSamenstelling(2, 3, 2, 2),
            new GroepSamenstelling(2, 2, 3, 2),
            new GroepSamenstelling(2, 2, 2, 3),
        })
        {
            int vereistMet = BkrCalculator.Bereken(metKind).VereisteHoeveelheidPmers;
            Assert.True(vereistMet >= vereistBasis,
                $"Toevoegen van een kind verlaagde het vereiste van {vereistBasis} naar {vereistMet}.");
        }
    }
}
