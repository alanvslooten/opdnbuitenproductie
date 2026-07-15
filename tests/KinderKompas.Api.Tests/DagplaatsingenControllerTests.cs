using KinderKompas.Api.Controllers;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Planning;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Tests;

/// <summary>
/// Verifieert het gedrag van de <see cref="DagplaatsingenController"/> tegen een
/// in-memory database: upsert per (kind, datum), afwezigheid, validatie en verwijderen.
/// </summary>
public sealed class DagplaatsingenControllerTests
{
    private sealed class VasteTenant : ITenantProvider
    {
        public Guid CurrentOrganisatieId => SeedConstanten.OrganisatieId;
    }

    private static readonly Guid Bengeltjes = SeedConstanten.StamgroepBengeltjesId;
    private static readonly Guid Boefjes = SeedConstanten.StamgroepBoefjesId;

    private static KinderKompasDbContext MaakContext()
    {
        var opties = new DbContextOptionsBuilder<KinderKompasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new KinderKompasDbContext(opties, new VasteTenant());
        db.Database.EnsureCreated(); // brengt de geseede stamgroepen (Bengeltjes/Boefjes) mee
        return db;
    }

    private static async Task<Kind> SeedKind(KinderKompasDbContext db)
    {
        var kind = new Kind
        {
            Voornaam = "Casey", Achternaam = "Test",
            Geboortedatum = new DateOnly(2024, 1, 1),
            StamgroepId = Bengeltjes,
            Startdatum = new DateOnly(2025, 1, 1),
            Contracttype = Contracttype.Weken49,
            GewensteOpvangdagen = Weekdag.Maandag | Weekdag.Woensdag,
        };
        db.Kinderen.Add(kind);
        await db.SaveChangesAsync();
        return kind;
    }

    private static DagplaatsingDto Waarde(ActionResult<DagplaatsingDto> resultaat)
        => Assert.IsType<DagplaatsingDto>(Assert.IsType<OkObjectResult>(resultaat.Result).Value);

    [Fact]
    public async Task Zetten_MaaktEenNieuweAfwijking_OpDeAndereGroep()
    {
        using var db = MaakContext();
        Kind kind = await SeedKind(db);
        var controller = new DagplaatsingenController(db);
        var datum = new DateOnly(2026, 3, 4);

        DagplaatsingDto dto = Waarde(await controller.Zetten(
            new DagplaatsingInvoer(kind.Id, datum, Boefjes, DagplaatsingSoort.Incidenteel, "kijkje bij Boefjes"),
            CancellationToken.None));

        Assert.Equal(Boefjes, dto.StamgroepId);
        Assert.True(dto.IsAanwezig);
        Assert.Equal(DagplaatsingSoort.Incidenteel, dto.Soort);
        Assert.Equal(1, await db.Dagplaatsingen.CountAsync());
    }

    [Fact]
    public async Task Zetten_TweeKeerOpDezelfdeDag_Overschrijft_GeenTweedeRij()
    {
        using var db = MaakContext();
        Kind kind = await SeedKind(db);
        var controller = new DagplaatsingenController(db);
        var datum = new DateOnly(2026, 3, 4);

        await controller.Zetten(new DagplaatsingInvoer(kind.Id, datum, Boefjes, DagplaatsingSoort.Incidenteel, null), CancellationToken.None);
        DagplaatsingDto tweede = Waarde(await controller.Zetten(
            new DagplaatsingInvoer(kind.Id, datum, null, DagplaatsingSoort.Afwezig, null), CancellationToken.None));

        Assert.Equal(1, await db.Dagplaatsingen.CountAsync());
        Assert.Null(tweede.StamgroepId);
        Assert.False(tweede.IsAanwezig);
        Assert.Equal(DagplaatsingSoort.Afwezig, tweede.Soort);
    }

    [Fact]
    public async Task Zetten_MetOnbekendKind_GeeftBadRequest()
    {
        using var db = MaakContext();
        var controller = new DagplaatsingenController(db);

        ActionResult<DagplaatsingDto> resultaat = await controller.Zetten(
            new DagplaatsingInvoer(Guid.NewGuid(), new DateOnly(2026, 3, 4), Boefjes, DagplaatsingSoort.Incidenteel, null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultaat.Result);
        Assert.Equal(0, await db.Dagplaatsingen.CountAsync());
    }

    [Fact]
    public async Task Zetten_MetOnbekendeStamgroep_GeeftBadRequest()
    {
        using var db = MaakContext();
        Kind kind = await SeedKind(db);
        var controller = new DagplaatsingenController(db);

        ActionResult<DagplaatsingDto> resultaat = await controller.Zetten(
            new DagplaatsingInvoer(kind.Id, new DateOnly(2026, 3, 4), Guid.NewGuid(), DagplaatsingSoort.Incidenteel, null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultaat.Result);
    }

    [Fact]
    public async Task Lijst_FiltertOpBereikEnKind()
    {
        using var db = MaakContext();
        Kind kind = await SeedKind(db);
        var controller = new DagplaatsingenController(db);
        await controller.Zetten(new DagplaatsingInvoer(kind.Id, new DateOnly(2026, 3, 4), Boefjes, DagplaatsingSoort.Incidenteel, null), CancellationToken.None);
        await controller.Zetten(new DagplaatsingInvoer(kind.Id, new DateOnly(2026, 4, 1), null, DagplaatsingSoort.Afwezig, null), CancellationToken.None);

        var inMaart = Assert.IsType<OkObjectResult>((await controller.Lijst(
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), null, CancellationToken.None)).Result).Value
            as IReadOnlyList<DagplaatsingDto>;

        Assert.NotNull(inMaart);
        DagplaatsingDto enige = Assert.Single(inMaart!);
        Assert.Equal(new DateOnly(2026, 3, 4), enige.Datum);
        Assert.Equal("Casey", enige.KindVoornaam);
    }

    [Fact]
    public async Task Verwijderen_HaaltDeAfwijkingWeg_OnbekendGeeftNotFound()
    {
        using var db = MaakContext();
        Kind kind = await SeedKind(db);
        var controller = new DagplaatsingenController(db);
        DagplaatsingDto dto = Waarde(await controller.Zetten(
            new DagplaatsingInvoer(kind.Id, new DateOnly(2026, 3, 4), Boefjes, DagplaatsingSoort.Incidenteel, null),
            CancellationToken.None));

        Assert.IsType<NoContentResult>(await controller.Verwijderen(dto.Id, CancellationToken.None));
        Assert.Equal(0, await db.Dagplaatsingen.CountAsync());
        Assert.IsType<NotFoundResult>(await controller.Verwijderen(Guid.NewGuid(), CancellationToken.None));
    }
}
