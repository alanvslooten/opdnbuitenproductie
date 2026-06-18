using KinderKompas.Application.Observaties;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Observaties;

/// <summary>
/// Bewijst dat het observatie-overzicht het domein-schema (geboortedatum-gedreven)
/// correct combineert met de reeds afgevinkte observaties: status per moment, de
/// telling per status, en de gekoppelde <see cref="ObservatieDto"/> bij een
/// afgevinkt moment.
/// </summary>
public class ObservatieOverzichtBouwerTests
{
    private static readonly DateOnly Peildatum = new(2026, 6, 18);

    private static Kind MaakKind() => new()
    {
        Voornaam = "Fenna",
        Achternaam = "de Vries",
        Geboortedatum = new DateOnly(2024, 1, 1), // ~29 maanden op de peildatum
        StamgroepId = Guid.NewGuid(),
        Startdatum = new DateOnly(2024, 3, 1),
        MentorId = Guid.NewGuid(),
    };

    private static Observatie MaakObservatie(Guid kindId, int mijlpaal) => new()
    {
        KindId = kindId,
        MijlpaalMaanden = mijlpaal,
        BestandsNaam = "observatie-6mnd.pdf",
        BestandsSleutel = "observaties/abc.pdf",
        ContentType = "application/pdf",
        BestandsGrootte = 12345,
    };

    [Fact]
    public void Bouw_GeeftAlleAchtMomenten_EnNeemtMentorOver()
    {
        Kind kind = MaakKind();

        KindObservatieschemaDto dto =
            ObservatieOverzichtBouwer.Bouw(kind, Array.Empty<Observatie>(), Peildatum);

        Assert.Equal(8, dto.Momenten.Count);
        Assert.Equal(kind.MentorId, dto.MentorId);
        Assert.Equal(kind.Geboortedatum.AddYears(4), dto.VierdeVerjaardag);
    }

    [Fact]
    public void AfgevinktMoment_IsAfgerond_MetGekoppeldeObservatie()
    {
        Kind kind = MaakKind();
        Observatie observatie = MaakObservatie(kind.Id, mijlpaal: 6);

        KindObservatieschemaDto dto =
            ObservatieOverzichtBouwer.Bouw(kind, new[] { observatie }, Peildatum);

        ObservatiemomentDto zesMaanden = dto.Momenten.Single(m => m.MijlpaalMaanden == 6);
        Assert.Equal(ObservatieStatus.Afgerond, zesMaanden.Status);
        Assert.NotNull(zesMaanden.Observatie);
        Assert.Equal("observatie-6mnd.pdf", zesMaanden.Observatie!.BestandsNaam);
        Assert.Equal(12345, zesMaanden.Observatie.BestandsGrootte);
    }

    [Fact]
    public void Telling_PerStatus_KloptOpDePeildatum()
    {
        Kind kind = MaakKind();
        // Alleen het 6-maandenmoment is afgevinkt.
        Observatie observatie = MaakObservatie(kind.Id, mijlpaal: 6);

        KindObservatieschemaDto dto =
            ObservatieOverzichtBouwer.Bouw(kind, new[] { observatie }, Peildatum);

        // Vervaldatums vanaf 2024-01-01: 6→2024-07 (afgevinkt), 12→2025-01,
        // 18→2025-07, 24→2026-01 (alle drie overschreden), 30→2026-07-01 (binnen 30
        // dagen → binnenkort), 36/42/46 → toekomst.
        Assert.Equal(1, dto.AantalAfgerond);
        Assert.Equal(3, dto.AantalOverschreden);
        Assert.Equal(1, dto.AantalBinnenkort);
    }
}
