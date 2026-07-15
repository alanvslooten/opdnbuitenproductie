using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Observaties;

/// <summary>
/// Borgt de kernregel van fase 7: observatiemomenten hangen aan de LEEFTIJD
/// (geboortedatum + vaste mijlpaal), niet aan "laatste observatie + 6 maanden".
/// De vaste mijlpalen zijn 6/12/18/24/30/36/42 maanden plus het eindmoment op
/// 3 jaar en 10 maanden (46 maanden), ~2 maanden vóór de 4e verjaardag.
/// </summary>
public class ObservatieschemaTests
{
    private static readonly IReadOnlySet<int> GeenAfgerond = new HashSet<int>();

    [Fact]
    public void Momenten_ZijnDeVasteAchtMijlpalen_OplopendOpLeeftijd()
    {
        int[] verwacht = { 6, 12, 18, 24, 30, 36, 42, 46 };

        int[] werkelijk = Observatieschema.Momenten.Select(m => m.MijlpaalMaanden).ToArray();

        Assert.Equal(verwacht, werkelijk);
    }

    [Fact]
    public void EindmomentOp46Maanden_IsAlsEindmomentGemarkeerd_EnDeRestNiet()
    {
        Observatiemoment eind = Observatieschema.Momenten.Single(m => m.MijlpaalMaanden == 46);
        Assert.True(eind.IsEindmoment);

        Assert.All(
            Observatieschema.Momenten.Where(m => m.MijlpaalMaanden != 46),
            m => Assert.False(m.IsEindmoment));
    }

    [Fact]
    public void Eindmoment_LigtPreciesTweeMaandenVoorDeVierdeVerjaardag()
    {
        DateOnly geboortedatum = new(2022, 8, 1);
        DateOnly vierdeVerjaardag = geboortedatum.AddYears(4); // 2026-08-01

        DateOnly eindVervaldatum = Observatieschema.VervaldatumVan(geboortedatum, Observatieschema.EindmomentMaanden);

        Assert.Equal(new DateOnly(2026, 6, 1), eindVervaldatum);
        Assert.Equal(vierdeVerjaardag, eindVervaldatum.AddMonths(2));
    }

    [Fact]
    public void Vervaldatum_IsGeboortedatumPlusMijlpaalInMaanden()
    {
        DateOnly geboortedatum = new(2024, 1, 15);

        IReadOnlyList<ObservatiemomentStatus> schema =
            Observatieschema.Bereken(geboortedatum, geboortedatum, GeenAfgerond);

        Assert.Equal(new DateOnly(2024, 7, 15), schema.Single(s => s.Moment.MijlpaalMaanden == 6).Vervaldatum);
        Assert.Equal(new DateOnly(2025, 1, 15), schema.Single(s => s.Moment.MijlpaalMaanden == 12).Vervaldatum);
        Assert.Equal(new DateOnly(2027, 7, 15), schema.Single(s => s.Moment.MijlpaalMaanden == 42).Vervaldatum);
    }

    [Fact]
    public void PasgeborenKind_HeeftAlleMomentenInDeToekomst_NietsOverschreden()
    {
        DateOnly geboortedatum = new(2026, 6, 18);

        IReadOnlyList<ObservatiemomentStatus> schema =
            Observatieschema.Bereken(geboortedatum, geboortedatum, GeenAfgerond);

        Assert.Equal(8, schema.Count);
        Assert.DoesNotContain(schema, s => s.Status == ObservatieStatus.Overschreden);
        // Het eerste moment (6 mnd) ligt ruim buiten de 30-dagen-drempel.
        Assert.Equal(ObservatieStatus.NogNietAanDeBeurt,
            schema.Single(s => s.Moment.MijlpaalMaanden == 6).Status);
    }

    [Fact]
    public void Moment_InHetVerleden_EnNietAfgevinkt_IsOverschreden()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        // 6-maandenmoment valt op 2024-07-01; peildatum ligt daar ruim na.
        DateOnly peildatum = new(2024, 10, 1);

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        Assert.Equal(ObservatieStatus.Overschreden, zesMaanden.Status);
    }

    [Fact]
    public void Moment_InHetVerleden_MaarAfgevinkt_IsAfgerond()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly peildatum = new(2024, 10, 1);
        var afgerond = new HashSet<int> { 6 };

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, afgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        Assert.Equal(ObservatieStatus.Afgerond, zesMaanden.Status);
    }

    [Fact]
    public void Moment_BinnenDeDrempel_IsBinnenkort_BuitenDeDrempel_NogNietAanDeBeurt()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        // 12-maandenmoment valt op 2025-01-01.
        DateOnly net20DagenVoor = new(2024, 12, 12);   // binnen 30 dagen
        DateOnly ruim40DagenVoor = new(2024, 11, 20);  // buiten 30 dagen

        ObservatiemomentStatus binnenkort = Observatieschema
            .Bereken(geboortedatum, net20DagenVoor, GeenAfgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 12);
        ObservatiemomentStatus teVroeg = Observatieschema
            .Bereken(geboortedatum, ruim40DagenVoor, GeenAfgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 12);

        Assert.Equal(ObservatieStatus.Binnenkort, binnenkort.Status);
        Assert.Equal(ObservatieStatus.NogNietAanDeBeurt, teVroeg.Status);
    }

    [Fact]
    public void Moment_OpDeVervaldatumZelf_IsBinnenkort()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly vervaldatum = new(2024, 7, 1); // exact het 6-maandenmoment

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, vervaldatum, GeenAfgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        Assert.Equal(ObservatieStatus.Binnenkort, zesMaanden.Status);
    }

    [Fact]
    public void AangepasteDrempel_VerschuiftDeBinnenkortGrens()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        // 12-maandenmoment op 2025-01-01; peildatum 50 dagen ervoor.
        DateOnly peildatum = new(2024, 11, 12);

        ObservatieStatus metStandaard = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond)
            .Single(s => s.Moment.MijlpaalMaanden == 12).Status;
        ObservatieStatus metRuimeDrempel = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond, binnenkortDrempelDagen: 60)
            .Single(s => s.Moment.MijlpaalMaanden == 12).Status;

        Assert.Equal(ObservatieStatus.NogNietAanDeBeurt, metStandaard);
        Assert.Equal(ObservatieStatus.Binnenkort, metRuimeDrempel);
    }

    [Fact]
    public void Eindmoment_TweeMaandenVoorVier_IsOverschredenAlsNietAfgevinkt()
    {
        // Kind wordt 4 op 2026-08-01; eindmoment valt op 2026-06-01.
        DateOnly geboortedatum = new(2022, 8, 1);
        DateOnly peildatum = new(2026, 6, 18);

        ObservatiemomentStatus eind = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond)
            .Single(s => s.Moment.IsEindmoment);

        Assert.Equal(46, eind.Moment.MijlpaalMaanden);
        Assert.Equal(ObservatieStatus.Overschreden, eind.Status);
    }

    [Fact]
    public void Beschrijving_IsLeesbaarNederlands()
    {
        Assert.Equal("6 maanden", new Observatiemoment(6, false).Beschrijving);
        Assert.Equal("1 jaar", new Observatiemoment(12, false).Beschrijving);
        Assert.Equal("1 jaar en 6 maanden", new Observatiemoment(18, false).Beschrijving);
        Assert.Equal("3 jaar en 10 maanden", new Observatiemoment(46, true).Beschrijving);
    }

    [Fact]
    public void Bereken_MetNullAfgerondeMijlpalen_Werpt()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Observatieschema.Bereken(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1), null!));
    }

    [Fact]
    public void KindDatLaterStart_KrijgtGeenOverschredenMomentVoorDeStartdatum()
    {
        // Kind is geboren 2024-01-01, maar start pas op 7 maanden (2024-08-01) bij de
        // opvang. Het 6-maandenmoment (2024-07-01) viel vóór de startdatum.
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly startdatum = new(2024, 8, 1);
        DateOnly peildatum = new(2024, 10, 1); // ruim ná het 6-maandenmoment

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond, startdatum: startdatum)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        // Zonder de startdatum zou dit "Overschreden" zijn (zie de test hierboven);
        // mét startdatum hoort het niet bij de opvang.
        Assert.Equal(ObservatieStatus.VoorStartdatum, zesMaanden.Status);
    }

    [Fact]
    public void MomentOpOfNaDeStartdatum_VolgtDeGewoneStatuslogica()
    {
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly startdatum = new(2024, 8, 1);
        // 12-maandenmoment (2025-01-01) ligt ná de startdatum; peildatum erna → overschreden.
        DateOnly peildatum = new(2025, 3, 1);

        ObservatiemomentStatus twaalfMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond, startdatum: startdatum)
            .Single(s => s.Moment.MijlpaalMaanden == 12);

        Assert.Equal(ObservatieStatus.Overschreden, twaalfMaanden.Status);
    }

    [Fact]
    public void AfgevinktMomentVoorStartdatum_BlijftAfgerond()
    {
        // Afvinken wint: als een moment vóór de startdatum tóch is afgerond, blijft dat zo.
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly startdatum = new(2024, 8, 1);
        DateOnly peildatum = new(2024, 10, 1);
        var afgerond = new HashSet<int> { 6 };

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, afgerond, startdatum: startdatum)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        Assert.Equal(ObservatieStatus.Afgerond, zesMaanden.Status);
    }

    [Fact]
    public void ZonderStartdatum_BlijftHetOudeGedrag_Overschreden()
    {
        // Terugvalgedrag: geen startdatum meegegeven → moment in het verleden is overschreden.
        DateOnly geboortedatum = new(2024, 1, 1);
        DateOnly peildatum = new(2024, 10, 1);

        ObservatiemomentStatus zesMaanden = Observatieschema
            .Bereken(geboortedatum, peildatum, GeenAfgerond, startdatum: null)
            .Single(s => s.Moment.MijlpaalMaanden == 6);

        Assert.Equal(ObservatieStatus.Overschreden, zesMaanden.Status);
    }
}
