using KinderKompas.Application.Planning;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
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
/// <see cref="Capabilities.MagKinderenBeheren"/>.
/// </summary>
[ApiController]
[Route("api/planning")]
[Authorize(Policy = Capabilities.MagKinderenBeheren)]
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

        var stamgroepen = await _db.Stamgroepen
            .AsNoTracking()
            .Include(s => s.Kinderen)
            .OrderBy(s => s.Naam)
            .ToListAsync(ct);

        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);

        WeekplanningDto week = WeekplanningBouwer.Bouw(peil, stamgroepen, vakanties);
        return Ok(week);
    }

    /// <summary>Welke kinderen zijn op de gegeven dag aanwezig (optioneel binnen één stamgroep).</summary>
    [HttpGet("dag")]
    public async Task<ActionResult<IReadOnlyList<AanwezigKindDto>>> Dag(
        [FromQuery] DateOnly? datum, [FromQuery] Guid? stamgroepId, CancellationToken ct)
    {
        DateOnly peil = datum ?? Vandaag;

        var query = _db.Kinderen.AsNoTracking();
        if (stamgroepId is { } gid)
        {
            query = query.Where(k => k.StamgroepId == gid);
        }

        List<Kind> kinderen = await query.ToListAsync(ct);
        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);

        IReadOnlyList<AanwezigKindDto> aanwezig = Aanwezigheid
            .AanwezigOp(kinderen, peil, vakanties)
            .Select(k => new AanwezigKindDto(
                k.Id, k.Voornaam, k.Achternaam, k.StamgroepId,
                k.LeeftijdscategorieOp(peil).Groep, k.Contracttype))
            .OrderBy(k => k.Achternaam).ThenBy(k => k.Voornaam)
            .ToList();

        return Ok(aanwezig);
    }
}
