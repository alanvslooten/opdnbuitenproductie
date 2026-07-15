using KinderKompas.Application.Planning;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Levert de afgeleide plannings-weergavedata: de weekplanning (per stamgroep, per dag,
/// met BKR) en een dagfilter ("wie is er maandag?"). Er wordt niets opgeslagen — dit is
/// input voor het rooster (fase 5). De aanwezigheids- en BKR-logica komt volledig uit het
/// domein; de controller laadt enkel data en mapt. Afgeschermd met
/// <see cref="Capabilities.MagPlanningZien"/> (alleen-lezen; los van kindbeheer).
/// </summary>
[ApiController]
[Route("api/planning")]
[Authorize(Policy = Capabilities.MagPlanningZien)]
public sealed class PlanningController : ControllerBase
{
    private readonly KinderKompasDbContext _db;

    public PlanningController(KinderKompasDbContext db)
    {
        _db = db;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>De weekplanning voor de week waarin <paramref name="datum"/> valt (default: deze week).</summary>
    [HttpGet("week")]
    public async Task<ActionResult<WeekplanningDto>> Week([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly peil = datum ?? Vandaag;
        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(peil);

        var stamgroepen = await _db.Stamgroepen
            .AsNoTracking()
            .Include(s => s.Kinderen)
            .OrderBy(s => s.Naam)
            .ToListAsync(ct);

        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        List<Roosterdienst> diensten = await VerstuurdeDienstenAsync(weekBegin, weekBegin.AddDays(4), ct);
        List<Dagplaatsing> dagplaatsingen = await DagplaatsingenAsync(weekBegin, weekBegin.AddDays(4), ct);

        WeekplanningDto week = WeekplanningBouwer.Bouw(peil, stamgroepen, vakanties, diensten, dagplaatsingen);
        return Ok(week);
    }

    /// <summary>
    /// De maandplanning (alleen-lezen): alle weken die de maand van <paramref name="datum"/>
    /// raken, elk als volwaardige weekplanning met kinderen, BKR en ingeplande begeleiders.
    /// </summary>
    [HttpGet("maand")]
    public async Task<ActionResult<MaandPlanningDto>> Maand([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly peil = datum ?? Vandaag;
        var eersteVanMaand = new DateOnly(peil.Year, peil.Month, 1);
        DateOnly laatsteVanMaand = eersteVanMaand.AddMonths(1).AddDays(-1);
        DateOnly start = WeekplanningBouwer.WeekBeginVan(eersteVanMaand);
        DateOnly eindWeekBegin = WeekplanningBouwer.WeekBeginVan(laatsteVanMaand);

        var stamgroepen = await _db.Stamgroepen
            .AsNoTracking()
            .Include(s => s.Kinderen)
            .OrderBy(s => s.Naam)
            .ToListAsync(ct);
        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        List<Roosterdienst> diensten = await VerstuurdeDienstenAsync(start, eindWeekBegin.AddDays(4), ct);
        List<Dagplaatsing> dagplaatsingen = await DagplaatsingenAsync(start, eindWeekBegin.AddDays(4), ct);

        var weken = new List<WeekplanningDto>();
        for (DateOnly w = start; w <= eindWeekBegin; w = w.AddDays(7))
        {
            weken.Add(WeekplanningBouwer.Bouw(w, stamgroepen, vakanties, diensten, dagplaatsingen));
        }

        return Ok(new MaandPlanningDto(peil.Year, peil.Month, weken));
    }

    /// <summary>Welke kinderen + begeleiders zijn op de gegeven dag aanwezig (optioneel binnen één stamgroep).</summary>
    [HttpGet("dag")]
    public async Task<ActionResult<DagFilterDto>> Dag(
        [FromQuery] DateOnly? datum, [FromQuery] Guid? stamgroepId, CancellationToken ct)
    {
        DateOnly peil = datum ?? Vandaag;

        // We laden ALLE kinderen (niet vooraf op thuisgroep gefilterd): een dagafwijking
        // kan een kind op een andere dan zijn thuisgroep zetten. De effectieve indeling
        // bepaalt vervolgens wie waar staat.
        List<Kind> kinderen = await _db.Kinderen.AsNoTracking().ToListAsync(ct);
        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        List<Dagplaatsing> dagplaatsingen = await DagplaatsingenAsync(peil, peil, ct);

        // Met een groepfilter: wie staat er die dag effectief op die groep. Zonder filter:
        // wie is er die dag überhaupt (op welke groep dan ook).
        IReadOnlyList<Kind> effectiefAanwezig = stamgroepId is { } gid
            ? Dagindeling.OpGroepOpDag(kinderen, gid, peil, dagplaatsingen, vakanties)
            : Dagindeling.AanwezigOp(kinderen, peil, dagplaatsingen, vakanties);

        IReadOnlyList<AanwezigKindDto> aanwezig = effectiefAanwezig
            .Select(k => new AanwezigKindDto(
                k.Id, k.Voornaam, k.Achternaam, k.StamgroepId,
                k.LeeftijdscategorieOp(peil).Groep, k.Contracttype))
            .OrderBy(k => k.Achternaam).ThenBy(k => k.Voornaam)
            .ToList();

        List<Roosterdienst> diensten = await VerstuurdeDienstenAsync(peil, peil, ct);
        IReadOnlyList<PlanningBegeleiderDto> begeleiders = diensten
            .Where(d => stamgroepId is not { } g || d.StamgroepId == g)
            .Select(d => new PlanningBegeleiderDto(
                d.MedewerkerId,
                d.Medewerker is null ? "" : $"{d.Medewerker.Voornaam} {d.Medewerker.Achternaam}",
                d.Taakomschrijving))
            .OrderBy(b => b.Naam)
            .ToList();

        return Ok(new DagFilterDto(aanwezig, begeleiders));
    }

    /// <summary>
    /// De diensten uit ENKEL verstuurde roosterweken binnen een datumbereik (incl. medewerker).
    /// Concept-roosters tellen niet als "aanwezig".
    /// </summary>
    private async Task<List<Roosterdienst>> VerstuurdeDienstenAsync(DateOnly van, DateOnly tot, CancellationToken ct) =>
        await _db.Roosterdiensten.AsNoTracking()
            .Include(d => d.Medewerker)
            .Where(d => d.Datum >= van && d.Datum <= tot && d.Roosterweek!.Status == RoosterStatus.Verstuurd)
            .ToListAsync(ct);

    /// <summary>De dagafwijkingen (dagplaatsingen) binnen een datumbereik.</summary>
    private async Task<List<Dagplaatsing>> DagplaatsingenAsync(DateOnly van, DateOnly tot, CancellationToken ct) =>
        await _db.Dagplaatsingen.AsNoTracking()
            .Where(d => d.Datum >= van && d.Datum <= tot)
            .ToListAsync(ct);
}
