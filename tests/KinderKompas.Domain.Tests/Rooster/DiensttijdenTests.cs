using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Rooster;

/// <summary>
/// Borgt de v3-diensttijdregels: standaardtijden per dienstsoort, de onbetaalde pauze
/// (1 uur bij een lange dienst, anders 0,5 uur) en de netto geplande uren.
/// </summary>
public class DiensttijdenTests
{
    [Theory]
    [InlineData(Dienstsoort.Vroege, 7, 30, 17, 30)]
    [InlineData(Dienstsoort.Regulier, 8, 0, 18, 0)]
    [InlineData(Dienstsoort.Late, 8, 30, 18, 0)]
    public void Standaard_GeeftDeAfgesprokenTijdenPerSoort(Dienstsoort soort, int bu, int bm, int eu, int em)
    {
        (TimeOnly begin, TimeOnly eind) = Diensttijden.Standaard(soort);

        Assert.Equal(new TimeOnly(bu, bm), begin);
        Assert.Equal(new TimeOnly(eu, em), eind);
    }

    [Fact]
    public void Pauze_LangeDienst_IsEenUur()
    {
        // 07:30–17:30 = 10 uur bruto → lange dienst.
        TimeSpan pauze = Diensttijden.Pauze(new TimeOnly(7, 30), new TimeOnly(17, 30));

        Assert.Equal(TimeSpan.FromHours(1), pauze);
    }

    [Fact]
    public void Pauze_KorteDienst_IseenHalfUur()
    {
        // 08:30–13:00 = 4,5 uur bruto → korte dienst.
        TimeSpan pauze = Diensttijden.Pauze(new TimeOnly(8, 30), new TimeOnly(13, 0));

        Assert.Equal(TimeSpan.FromMinutes(30), pauze);
    }

    [Fact]
    public void Pauze_OpDrempelVanZesUur_IsAlLang()
    {
        TimeSpan pauze = Diensttijden.Pauze(new TimeOnly(8, 0), new TimeOnly(14, 0)); // precies 6 uur

        Assert.Equal(TimeSpan.FromHours(1), pauze);
    }

    [Fact]
    public void NettoUren_LangeDienst_TrektEenUurPauzeAf()
    {
        // 07:30–17:30 = 10 uur bruto − 1 uur pauze = 9 uur netto.
        decimal netto = Diensttijden.NettoUren(new TimeOnly(7, 30), new TimeOnly(17, 30));

        Assert.Equal(9m, netto);
    }

    [Fact]
    public void NettoUren_KorteDienst_TrekteenHalfUurAf()
    {
        // 08:30–13:00 = 4,5 uur bruto − 0,5 uur = 4 uur netto.
        decimal netto = Diensttijden.NettoUren(new TimeOnly(8, 30), new TimeOnly(13, 0));

        Assert.Equal(4m, netto);
    }

    [Fact]
    public void NettoUren_EindVoorBegin_IsNul()
    {
        Assert.Equal(0m, Diensttijden.NettoUren(new TimeOnly(18, 0), new TimeOnly(8, 0)));
    }
}
