using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Instellingen;

/// <summary>
/// Bewijst de instellingen-helpers (fase 9c): de defaults spiegelen de code-constanten,
/// de meldingen-zichtbaarheid wordt correct geparset/genormaliseerd, en het verbergen
/// van een soort verandert alleen de UI-zichtbaarheid (geen andere soort raakt geraakt).
/// </summary>
public class OrganisatieInstellingenTests
{
    [Fact]
    public void Defaults_spiegelen_de_code_constanten()
    {
        var i = new OrganisatieInstellingen();

        Assert.Equal(Observatieschema.StandaardBinnenkortDrempelDagen, i.ObservatieBinnenkortDrempelDagen);
        Assert.Equal(OrganisatieInstellingen.StandaardKindBinnenkortVierDagen, i.KindBinnenkortVierDrempelDagen);
        Assert.Equal(WachtlijstPrioriteit.PuntenIntern, i.PrioriteitInternGewicht);
        Assert.Equal(WachtlijstPrioriteit.PuntenPerMaandWachtend, i.PrioriteitPerMaandGewicht);
        Assert.Empty(i.VerborgenSoorten());
        Assert.Null(i.StandaardObservatietekst);
    }

    [Fact]
    public void Standaard_zijn_alle_soorten_zichtbaar()
    {
        var i = new OrganisatieInstellingen();

        foreach (MeldingSoort soort in Enum.GetValues<MeldingSoort>())
        {
            Assert.True(i.IsSoortZichtbaar(soort));
        }
    }

    [Fact]
    public void Verbergen_van_een_soort_raakt_alleen_die_soort()
    {
        var i = new OrganisatieInstellingen();

        i.ZetVerborgenSoorten(new[] { MeldingSoort.BkrWaarschuwing, MeldingSoort.Ziekmelding });

        Assert.False(i.IsSoortZichtbaar(MeldingSoort.BkrWaarschuwing));
        Assert.False(i.IsSoortZichtbaar(MeldingSoort.Ziekmelding));
        Assert.True(i.IsSoortZichtbaar(MeldingSoort.Verlofaanvraag));
        Assert.True(i.IsSoortZichtbaar(MeldingSoort.VoorstelGeaccepteerd));
    }

    [Fact]
    public void ZetVerborgenSoorten_normaliseert_uniek_en_gesorteerd()
    {
        var i = new OrganisatieInstellingen();

        i.ZetVerborgenSoorten(new[]
        {
            MeldingSoort.Ziekmelding,           // 3
            MeldingSoort.BkrWaarschuwing,        // 0
            MeldingSoort.Ziekmelding,            // dubbel
        });

        // Genormaliseerd: gesorteerd op nummer, zonder duplicaten.
        Assert.Equal("0,3", i.VerborgenMeldingsoorten);
        Assert.Equal(2, i.VerborgenSoorten().Count);
    }

    [Fact]
    public void Parsen_negeert_rommel_en_onbekende_waarden()
    {
        var i = new OrganisatieInstellingen { VerborgenMeldingsoorten = "2, ,9999,abc,3" };

        IReadOnlySet<MeldingSoort> soorten = i.VerborgenSoorten();

        Assert.Contains(MeldingSoort.Verlofaanvraag, soorten); // 2
        Assert.Contains(MeldingSoort.Ziekmelding, soorten);    // 3
        Assert.Equal(2, soorten.Count);                         // 9999/abc/leeg genegeerd
    }
}
