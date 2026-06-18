using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Wachtlijst;

/// <summary>
/// Bewijst dat <see cref="GroepSamenstelling.MetExtra"/> precies één kind in de
/// juiste leeftijdsgroep toevoegt en dat de BKR-impact daarmee gegarandeerd uit
/// de wettelijke rekenkern komt (geen aparte "wat-als"-logica).
/// </summary>
public class GroepSamenstellingMetExtraTests
{
    [Fact]
    public void MetExtra_VerhoogtAlleenDeGekozenGroep()
    {
        var basis = new GroepSamenstelling(aantalNulTotEen: 2, aantalEenTotTwee: 1, 0, 0);

        var na = basis.MetExtra(Leeftijdsgroep.NulTotEen);

        Assert.Equal(3, na.AantalNulTotEen);
        Assert.Equal(1, na.AantalEenTotTwee);
        Assert.Equal(basis.Totaal + 1, na.Totaal);
    }

    [Fact]
    public void MetExtra_BkrImpact_KomtUitDeDomeinCalculator()
    {
        // 3 baby's → ceil(3/3) = 1 pm'er. Eén baby erbij → ceil(4/3) = 2 pm'ers.
        var basis = new GroepSamenstelling(3, 0, 0, 0);

        BkrUitkomst nu = BkrCalculator.Bereken(basis);
        BkrUitkomst na = BkrCalculator.Bereken(basis.MetExtra(Leeftijdsgroep.NulTotEen));

        Assert.Equal(1, nu.VereisteHoeveelheidPmers);
        Assert.Equal(2, na.VereisteHoeveelheidPmers);
    }
}
