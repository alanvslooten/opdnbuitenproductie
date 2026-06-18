using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Domain.Tests.Verlof;

/// <summary>
/// Bewijst de datum-omvatting van verlof en ziekte: een verlofbereik is inclusief
/// begin- en einddatum, en een ziekmelding zonder einddatum (nog niet hersteld)
/// telt vanaf de begindatum onbepaald door.
/// </summary>
public class VerlofEnZiekteDatumTests
{
    [Theory]
    [InlineData("2026-07-01", true)]   // begindatum (inclusief)
    [InlineData("2026-07-03", true)]   // midden
    [InlineData("2026-07-05", true)]   // einddatum (inclusief)
    [InlineData("2026-06-30", false)]  // dag ervoor
    [InlineData("2026-07-06", false)]  // dag erna
    public void Verlofaanvraag_OmvatDatum(string datum, bool verwacht)
    {
        var aanvraag = new Verlofaanvraag
        {
            Begindatum = new DateOnly(2026, 7, 1),
            Einddatum = new DateOnly(2026, 7, 5),
        };

        Assert.Equal(verwacht, aanvraag.OmvatDatum(DateOnly.Parse(datum)));
    }

    [Fact]
    public void Ziekmelding_met_open_einde_telt_onbepaald_door()
    {
        var ziek = new Ziekmelding
        {
            Begindatum = new DateOnly(2026, 7, 1),
            Einddatum = null,
        };

        Assert.False(ziek.OmvatDatum(new DateOnly(2026, 6, 30)));
        Assert.True(ziek.OmvatDatum(new DateOnly(2026, 7, 1)));
        Assert.True(ziek.OmvatDatum(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Ziekmelding_met_einddatum_is_begrensd()
    {
        var ziek = new Ziekmelding
        {
            Begindatum = new DateOnly(2026, 7, 1),
            Einddatum = new DateOnly(2026, 7, 4),
        };

        Assert.True(ziek.OmvatDatum(new DateOnly(2026, 7, 4)));
        Assert.False(ziek.OmvatDatum(new DateOnly(2026, 7, 5)));
    }
}
