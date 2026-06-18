using KinderKompas.Application.Observaties;

namespace KinderKompas.Application.Tests.Observaties;

/// <summary>
/// Bewijst dat de standaard observatie-mailtekst instelbaar is (fase 9c): een
/// ingesteld sjabloon vervangt <c>{voornaam}</c>, en zonder sjabloon (null/leeg) geldt
/// de ingebouwde standaardtekst.
/// </summary>
public class ObservatieMailTekstTests
{
    [Fact]
    public void Sjabloon_vervangt_de_voornaam_plaatshouder()
    {
        string tekst = ObservatieMailTekst.Bericht("Mees", "Hallo, dit gaat over {voornaam}.");

        Assert.Equal("Hallo, dit gaat over Mees.", tekst);
    }

    [Fact]
    public void Sjabloon_plaatshouder_is_hoofdletterongevoelig()
    {
        string tekst = ObservatieMailTekst.Bericht("Mees", "Beste ouder van {Voornaam}");

        Assert.Equal("Beste ouder van Mees", tekst);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Zonder_sjabloon_geldt_de_standaardtekst(string? sjabloon)
    {
        string tekst = ObservatieMailTekst.Bericht("Mees", sjabloon);

        Assert.Equal(ObservatieMailTekst.Bericht("Mees"), tekst);
        Assert.Contains("Mees", tekst);
    }
}
