using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Planning;

/// <summary>
/// Bewijst de v3-dagplaatsing: de EFFECTIEVE groepsindeling per dag houdt rekening met
/// dagafwijkingen (<see cref="Dagplaatsing"/>) bovenop het reguliere opvangpatroon.
/// Zonder afwijkingen valt <see cref="Dagindeling"/> samen met <see cref="Aanwezigheid"/>.
/// </summary>
public class DagindelingTests
{
    private const Weekdag AlleWeekdagen =
        Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag | Weekdag.Donderdag | Weekdag.Vrijdag;

    private static readonly Guid GroepA = Guid.NewGuid();
    private static readonly Guid GroepB = Guid.NewGuid();

    // Een gewone woensdag, ruim binnen de opvangleeftijd en contractperiode.
    private static readonly DateOnly Woensdag = new(2026, 3, 4);

    private static readonly Schoolvakantie[] GeenVakanties = Array.Empty<Schoolvakantie>();

    private static Kind MaakKind(
        Guid thuisgroep,
        Weekdag opvangdagen = AlleWeekdagen,
        DateOnly? startdatum = null) => new()
    {
        Id = Guid.NewGuid(),
        Voornaam = "Test",
        Achternaam = "Kind",
        Geboortedatum = new DateOnly(2024, 1, 1), // ~2 jaar op de peildatum
        Contracttype = Contracttype.Weken49,
        Startdatum = startdatum ?? new DateOnly(2025, 1, 1),
        GewensteOpvangdagen = opvangdagen,
        StamgroepId = thuisgroep,
    };

    private static Dagplaatsing Afwijking(Kind kind, DateOnly datum, Guid? groep, DagplaatsingSoort soort) => new()
    {
        KindId = kind.Id,
        Datum = datum,
        StamgroepId = groep,
        Soort = soort,
    };

    [Fact]
    public void ZonderAfwijkingen_VoltHetRegulierePatroon_ThuisgroepTelt()
    {
        Kind kind = MaakKind(GroepA);

        Guid? groep = Dagindeling.EffectieveGroepOp(kind, Woensdag, afwijking: null, GeenVakanties);

        Assert.Equal(GroepA, groep);
    }

    [Fact]
    public void ZonderAfwijkingen_OpGroepOpDag_GelijkAanThuisgroepAanwezigheid()
    {
        var kinderen = new[]
        {
            MaakKind(GroepA), MaakKind(GroepA), MaakKind(GroepB),
        };

        IReadOnlyList<Kind> opA =
            Dagindeling.OpGroepOpDag(kinderen, GroepA, Woensdag, Array.Empty<Dagplaatsing>(), GeenVakanties);
        IReadOnlyList<Kind> opB =
            Dagindeling.OpGroepOpDag(kinderen, GroepB, Woensdag, Array.Empty<Dagplaatsing>(), GeenVakanties);

        Assert.Equal(2, opA.Count);
        Assert.Single(opB);
    }

    [Fact]
    public void IncidenteelOpAndereGroep_TeltBijDeAndereGroep_NietBijDeThuisgroep()
    {
        Kind kind = MaakKind(GroepA);
        var afwijkingen = new[] { Afwijking(kind, Woensdag, GroepB, DagplaatsingSoort.Incidenteel) };

        IReadOnlyList<Kind> opA =
            Dagindeling.OpGroepOpDag(new[] { kind }, GroepA, Woensdag, afwijkingen, GeenVakanties);
        IReadOnlyList<Kind> opB =
            Dagindeling.OpGroepOpDag(new[] { kind }, GroepB, Woensdag, afwijkingen, GeenVakanties);

        Assert.Empty(opA);
        Assert.Single(opB);
    }

    [Fact]
    public void Afwezig_HeftEenRegulariereOpvangdagOp_KindTeltNergens()
    {
        Kind kind = MaakKind(GroepA);
        var afwijkingen = new[] { Afwijking(kind, Woensdag, groep: null, DagplaatsingSoort.Afwezig) };

        Guid? groep = Dagindeling.EffectieveGroepOp(kind, Woensdag, afwijkingen[0], GeenVakanties);
        IReadOnlyList<Kind> opA =
            Dagindeling.OpGroepOpDag(new[] { kind }, GroepA, Woensdag, afwijkingen, GeenVakanties);

        Assert.Null(groep);
        Assert.Empty(opA);
    }

    [Fact]
    public void ExtraDag_MaaktKindAanwezigOpEenDagBuitenHetPatroon()
    {
        // Kind komt regulier alleen op maandag; woensdag is geen opvangdag.
        Kind kind = MaakKind(GroepA, opvangdagen: Weekdag.Maandag);
        Assert.Null(Dagindeling.EffectieveGroepOp(kind, Woensdag, afwijking: null, GeenVakanties));

        var afwijking = Afwijking(kind, Woensdag, GroepA, DagplaatsingSoort.ExtraDag);
        Guid? metExtra = Dagindeling.EffectieveGroepOp(kind, Woensdag, afwijking, GeenVakanties);

        Assert.Equal(GroepA, metExtra);
    }

    [Fact]
    public void Ruildag_VerplaatstDeTellingVanDeEneDagNaarDeAndere()
    {
        // Kind komt regulier op dinsdag én woensdag op GroepA. Deze week ruilt het
        // woensdag in voor een dag op GroepB (en is woensdag afwezig op A).
        Kind kind = MaakKind(GroepA, opvangdagen: Weekdag.Dinsdag | Weekdag.Woensdag);
        DateOnly dinsdag = new(2026, 3, 3);
        DateOnly woensdag = Woensdag; // 2026-03-04

        var afwijkingen = new[]
        {
            Afwijking(kind, woensdag, groep: null, DagplaatsingSoort.Afwezig),
            Afwijking(kind, dinsdag, GroepA, DagplaatsingSoort.Ruildag), // blijft op A, dinsdag
        };

        // Woensdag: niet meer op A.
        Assert.Empty(Dagindeling.OpGroepOpDag(new[] { kind }, GroepA, woensdag, afwijkingen, GeenVakanties));
        // Dinsdag: nog steeds op A.
        Assert.Single(Dagindeling.OpGroepOpDag(new[] { kind }, GroepA, dinsdag, afwijkingen, GeenVakanties));
    }

    [Fact]
    public void AfwijkingGeldtAlleenVoorDeEigenDatum_NietVoorAndereDagen()
    {
        Kind kind = MaakKind(GroepA);
        DateOnly andereWoensdag = Woensdag.AddDays(7);
        var afwijkingen = new[] { Afwijking(kind, Woensdag, GroepB, DagplaatsingSoort.Incidenteel) };

        // Op de afwijkingsdatum: op B. Een week later (geen afwijking): weer op de thuisgroep A.
        Assert.Single(Dagindeling.OpGroepOpDag(new[] { kind }, GroepB, Woensdag, afwijkingen, GeenVakanties));
        Assert.Single(Dagindeling.OpGroepOpDag(new[] { kind }, GroepA, andereWoensdag, afwijkingen, GeenVakanties));
    }

    [Fact]
    public void KindVoorZijnStartdatum_ZonderAfwijking_TeltNietMee()
    {
        // Casey-scenario: start pas later; geen dagplaatsing nodig.
        Kind kind = MaakKind(GroepA, startdatum: Woensdag.AddMonths(1));

        Guid? groep = Dagindeling.EffectieveGroepOp(kind, Woensdag, afwijking: null, GeenVakanties);

        Assert.Null(groep);
    }

    [Fact]
    public void SamenstellingOpGroepOpDag_LevertDeBkrInputVanDeEffectiefAanwezigeKinderen()
    {
        // Twee kinderen thuis op A; één wordt incidenteel naar B verplaatst.
        Kind blijftOpA = MaakKind(GroepA);
        Kind naarB = MaakKind(GroepA);
        var kinderen = new[] { blijftOpA, naarB };
        var afwijkingen = new[] { Afwijking(naarB, Woensdag, GroepB, DagplaatsingSoort.Incidenteel) };

        GroepSamenstelling samenstellingA =
            Dagindeling.SamenstellingOpGroepOpDag(kinderen, GroepA, Woensdag, afwijkingen, GeenVakanties);
        GroepSamenstelling samenstellingB =
            Dagindeling.SamenstellingOpGroepOpDag(kinderen, GroepB, Woensdag, afwijkingen, GeenVakanties);

        Assert.Equal(1, samenstellingA.Totaal);
        Assert.Equal(1, samenstellingB.Totaal);
    }

    [Fact]
    public void AanwezigOp_TeltAanwezigOpWelkeGroepDanOok_MaarNietDeAfwezigen()
    {
        Kind blijft = MaakKind(GroepA);          // regulier aanwezig op A
        Kind flexNaarB = MaakKind(GroepA);       // incidenteel naar B
        Kind afwezig = MaakKind(GroepA);         // afwezig-afwijking
        var kinderen = new[] { blijft, flexNaarB, afwezig };
        var afwijkingen = new[]
        {
            Afwijking(flexNaarB, Woensdag, GroepB, DagplaatsingSoort.Incidenteel),
            Afwijking(afwezig, Woensdag, groep: null, DagplaatsingSoort.Afwezig),
        };

        IReadOnlyList<Kind> aanwezig =
            Dagindeling.AanwezigOp(kinderen, Woensdag, afwijkingen, GeenVakanties);

        // 'blijft' (op A) en 'flexNaarB' (op B) tellen; 'afwezig' niet.
        Assert.Equal(2, aanwezig.Count);
        Assert.Contains(blijft, aanwezig);
        Assert.Contains(flexNaarB, aanwezig);
        Assert.DoesNotContain(afwezig, aanwezig);
    }
}
