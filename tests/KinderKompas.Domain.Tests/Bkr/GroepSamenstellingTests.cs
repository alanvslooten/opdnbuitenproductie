using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Bkr;

public class GroepSamenstellingTests
{
    [Fact]
    public void Totaal_telt_alle_categorieen_op()
    {
        var s = new GroepSamenstelling(2, 3, 4, 2);

        Assert.Equal(11, s.Totaal);
        Assert.False(s.IsLeeg);
        Assert.True(s.BevatBabys);
        Assert.True(s.IsGemengd);
        Assert.False(s.IsEnkeleLeeftijd);
    }

    [Fact]
    public void EnkeleLeeftijd_wordt_herkend()
    {
        var s = new GroepSamenstelling(0, 0, 8, 0);

        Assert.True(s.IsEnkeleLeeftijd);
        Assert.False(s.IsGemengd);
        Assert.False(s.BevatBabys);
        Assert.Equal(new[] { Leeftijdsgroep.TweeTotDrie }, s.AanwezigeGroepen);
    }

    [Fact]
    public void NegatiefAantal_gooit_exception()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GroepSamenstelling(-1, 0, 0, 0));
    }

    [Fact]
    public void VanafGeboortedata_deelt_kinderen_in_op_basis_van_peildatum()
    {
        var peildatum = new DateOnly(2026, 6, 17);
        var geboortedata = new[]
        {
            new DateOnly(2026, 1, 1),  // 0 jaar  -> 0-1
            new DateOnly(2025, 1, 1),  // 1 jaar  -> 1-2
            new DateOnly(2024, 1, 1),  // 2 jaar  -> 2-3
            new DateOnly(2023, 1, 1),  // 3 jaar  -> 3-4
            new DateOnly(2026, 3, 1),  // 0 jaar  -> 0-1
        };

        var s = GroepSamenstelling.VanafGeboortedata(geboortedata, peildatum);

        Assert.Equal(2, s.AantalNulTotEen);
        Assert.Equal(1, s.AantalEenTotTwee);
        Assert.Equal(1, s.AantalTweeTotDrie);
        Assert.Equal(1, s.AantalDrieTotVier);
        Assert.Equal(5, s.Totaal);
    }

    [Fact]
    public void AantalIn_geeft_aantal_per_groep()
    {
        var s = new GroepSamenstelling(2, 3, 4, 2);

        Assert.Equal(2, s.AantalIn(Leeftijdsgroep.NulTotEen));
        Assert.Equal(3, s.AantalIn(Leeftijdsgroep.EenTotTwee));
        Assert.Equal(4, s.AantalIn(Leeftijdsgroep.TweeTotDrie));
        Assert.Equal(2, s.AantalIn(Leeftijdsgroep.DrieTotVier));
    }
}
