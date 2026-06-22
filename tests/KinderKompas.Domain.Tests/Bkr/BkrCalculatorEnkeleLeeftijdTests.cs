using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Bkr;

/// <summary>
/// De locatie-/snelrekenvariant <see cref="BkrCalculator.VereisteVoorEnkeleLeeftijd"/>:
/// vereiste pm'ers voor kinderen van één leeftijd op basis van de ratio, ZONDER
/// groepsmaximum (dezelfde ratio blijft gelden over meerdere stamgroepen). Voedt de
/// BKR-snelrekenaar.
/// </summary>
public class BkrCalculatorEnkeleLeeftijdTests
{
    [Theory]
    // 0-1 jaar → ratio 3
    [InlineData(Leeftijdsgroep.NulTotEen, 0, 0)]
    [InlineData(Leeftijdsgroep.NulTotEen, 3, 1)]
    [InlineData(Leeftijdsgroep.NulTotEen, 6, 2)]
    [InlineData(Leeftijdsgroep.NulTotEen, 7, 3)]
    // 1-2 jaar → ratio 5
    [InlineData(Leeftijdsgroep.EenTotTwee, 5, 1)]
    [InlineData(Leeftijdsgroep.EenTotTwee, 11, 3)]
    // 2-3 jaar → ratio 8 (wettelijk, NIET de 6 uit het oude prototype)
    [InlineData(Leeftijdsgroep.TweeTotDrie, 8, 1)]
    [InlineData(Leeftijdsgroep.TweeTotDrie, 10, 2)]
    // 3-4 jaar → ratio 8
    [InlineData(Leeftijdsgroep.DrieTotVier, 8, 1)]
    [InlineData(Leeftijdsgroep.DrieTotVier, 9, 2)]
    public void Geeft_juiste_aantal_pmers(Leeftijdsgroep groep, int aantal, int verwacht)
    {
        Assert.Equal(verwacht, BkrCalculator.VereisteVoorEnkeleLeeftijd(groep, aantal));
    }

    [Fact]
    public void Kent_geen_groepsmaximum_en_gooit_niet_bij_grote_aantallen()
    {
        // 13 baby's > wettelijk groepsmaximum (12) voor één fysieke groep, maar de
        // snelrekenaar mag gewoon doorrekenen: ceil(13/3) = 5.
        Assert.Equal(5, BkrCalculator.VereisteVoorEnkeleLeeftijd(Leeftijdsgroep.NulTotEen, 13));
    }

    [Fact]
    public void Negatief_aantal_is_ongeldig()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BkrCalculator.VereisteVoorEnkeleLeeftijd(Leeftijdsgroep.NulTotEen, -1));
    }
}
