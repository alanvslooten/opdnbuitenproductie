using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Domain.Tests.Portaal;

/// <summary>
/// De urenregistratie van het Groepsportaal (fase 8) telt de gewerkte tijd in hele
/// kwartieren, afgerond op het dichtstbijzijnde kwartier — net als de urencorrectie
/// op de roosterdienst, zodat het saldo in kwartieren blijft.
/// </summary>
public class UrenregistratieTests
{
    private static readonly DateTime Start = new(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Open_Registratie_GeeftNulKwartieren()
    {
        var reg = new Urenregistratie { Ingeklokt = Start, Uitgeklokt = null };

        Assert.True(reg.IsOpen);
        Assert.Equal(0, reg.GewerkteKwartieren);
        Assert.Equal(0m, reg.GewerkteUren);
    }

    [Theory]
    [InlineData(0, 0, 0)]      // exact inklok = uitklok → 0
    [InlineData(2, 0, 8)]      // 2 uur → 8 kwartieren
    [InlineData(0, 45, 3)]     // 45 min → 3 kwartieren
    [InlineData(0, 53, 4)]     // 53 min → 3,53 → afgerond 4 kwartieren
    [InlineData(0, 7, 0)]      // 7 min → 0,47 → afgerond 0 kwartieren
    [InlineData(7, 30, 30)]    // 7,5 uur → 30 kwartieren
    public void GewerkteKwartieren_RondtAfOpHeelKwartier(int uren, int minuten, int verwacht)
    {
        var reg = new Urenregistratie
        {
            Ingeklokt = Start,
            Uitgeklokt = Start.AddHours(uren).AddMinutes(minuten),
        };

        Assert.False(reg.IsOpen);
        Assert.Equal(verwacht, reg.GewerkteKwartieren);
        Assert.Equal(verwacht / 4m, reg.GewerkteUren);
    }

    [Fact]
    public void Uitklok_VoorInklok_TeltNietNegatief()
    {
        var reg = new Urenregistratie { Ingeklokt = Start, Uitgeklokt = Start.AddHours(-1) };

        Assert.Equal(0, reg.GewerkteKwartieren);
    }
}
