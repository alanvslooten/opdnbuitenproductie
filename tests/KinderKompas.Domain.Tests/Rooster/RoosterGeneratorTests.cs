using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Rooster;

/// <summary>
/// Bewijst de kernregels van de auto-rooster-generator (fase 5c): BKR-behoefte is
/// leidend (beschikbaarheid leidt niet vanzelf tot inplannen), goedgekeurd verlof en
/// ziekte worden ALTIJD gerespecteerd, de vaste bezetting wordt niet getrimd bij
/// overbezetting, en niemand wordt dubbel ingezet op dezelfde dag.
/// </summary>
public class RoosterGeneratorTests
{
    private static readonly Guid GroepA = Guid.NewGuid();
    private static readonly Guid GroepB = Guid.NewGuid();
    private static readonly DateOnly Maandag = new(2026, 6, 15); // een maandag

    private const Weekdag MaDiWo = Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag;

    private static Medewerker Vast(string achternaam, Guid groep, Weekdag vast = MaDiWo, Weekdag beschikbaar = Weekdag.Geen) => new()
    {
        Id = Guid.NewGuid(),
        Voornaam = "M",
        Achternaam = achternaam,
        VasteStamgroepId = groep,
        VasteWerkdagen = vast,
        Beschikbaarheidsdagen = beschikbaar,
    };

    private static Verlofaanvraag Verlof(Guid medewerkerId, DateOnly van, DateOnly tot) => new()
    {
        MedewerkerId = medewerkerId,
        Begindatum = van,
        Einddatum = tot,
        Status = VerlofStatus.Goedgekeurd,
        AantalUren = 8,
    };

    [Fact]
    public void Vaste_bezetting_wordt_geplaatst()
    {
        var sanne = Vast("Aaltink", GroepA);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne }, behoeften, Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());

        Assert.Single(regels);
        Assert.Equal(sanne.Id, regels[0].MedewerkerId);
        Assert.Equal(RoosterBron.Vast, regels[0].Bron);
    }

    [Fact]
    public void Plant_niemand_op_goedgekeurd_verlof()
    {
        var sanne = Vast("Aaltink", GroepA);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };
        var verlof = new[] { Verlof(sanne.Id, Maandag, Maandag) };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne }, behoeften, verlof, Array.Empty<Ziekmelding>());

        Assert.DoesNotContain(regels, r => r.MedewerkerId == sanne.Id);
    }

    [Fact]
    public void Zieke_medewerker_wordt_niet_ingepland()
    {
        var sanne = Vast("Aaltink", GroepA);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };
        var ziek = new[] { new Ziekmelding { MedewerkerId = sanne.Id, Begindatum = Maandag, Einddatum = null } };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne }, behoeften, Array.Empty<Verlofaanvraag>(), ziek);

        Assert.Empty(regels);
    }

    [Fact]
    public void Tekort_wordt_aangevuld_vanuit_beschikbaarheid()
    {
        // Vaste leidster ziek; nodig = 1; een beschikbare kracht vult op.
        var sanne = Vast("Aaltink", GroepA);
        var bram = Vast("Bakker", GroepB, vast: Weekdag.Geen, beschikbaar: MaDiWo);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };
        var ziek = new[] { new Ziekmelding { MedewerkerId = sanne.Id, Begindatum = Maandag, Einddatum = Maandag } };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne, bram }, behoeften, Array.Empty<Verlofaanvraag>(), ziek);

        Assert.Single(regels);
        Assert.Equal(bram.Id, regels[0].MedewerkerId);
        Assert.Equal(RoosterBron.Beschikbaar, regels[0].Bron);
    }

    [Fact]
    public void BKR_is_leidend_beschikbare_kracht_blijft_thuis_als_er_geen_tekort_is()
    {
        // Nodig = 1, vaste bezetting = 1 (genoeg). Beschikbare kracht wordt NIET ingepland.
        var sanne = Vast("Aaltink", GroepA);
        var bram = Vast("Bakker", GroepA, vast: Weekdag.Geen, beschikbaar: MaDiWo);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne, bram }, behoeften, Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());

        Assert.Single(regels);
        Assert.Equal(sanne.Id, regels[0].MedewerkerId);
    }

    [Fact]
    public void Vaste_bezetting_wordt_niet_getrimd_bij_overbezetting()
    {
        // Twee vaste leidsters, BKR vraagt er maar 1 -> beide blijven (Gail beslist).
        var sanne = Vast("Aaltink", GroepA);
        var nora = Vast("Berg", GroepA);
        var behoeften = new[] { new GroepDagBehoefte(GroepA, Maandag, 1) };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { sanne, nora }, behoeften, Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());

        Assert.Equal(2, regels.Count);
        Assert.All(regels, r => Assert.Equal(RoosterBron.Vast, r.Bron));
    }

    [Fact]
    public void Geen_dubbele_inzet_op_dezelfde_dag()
    {
        // Bram is vast in B en beschikbaar; A heeft een tekort. Bram is al vast ingezet
        // in B en mag niet óók in A worden bijgeplaatst.
        var bram = Vast("Bakker", GroepB, vast: MaDiWo, beschikbaar: MaDiWo);
        var behoeften = new[]
        {
            new GroepDagBehoefte(GroepA, Maandag, 1),
            new GroepDagBehoefte(GroepB, Maandag, 1),
        };

        var regels = RoosterGenerator.GenereerVoorstel(
            new[] { bram }, behoeften, Array.Empty<Verlofaanvraag>(), Array.Empty<Ziekmelding>());

        Assert.Single(regels);
        Assert.Equal(GroepB, regels[0].StamgroepId);
    }
}
