using KinderKompas.Application.Planning;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van dagafwijkingen (dagplaatsingen): een kind incidenteel op een andere groep,
/// een ruildag, een extra dag of een afwezigheid op één specifieke dag. De vaste thuisgroep
/// (<see cref="Kind.StamgroepId"/>) blijft ongewijzigd; deze afwijkingen sturen de planning
/// en BKR per dag (zie <see cref="Domain.Services.Dagindeling"/>).
///
/// Lezen valt onder <see cref="Capabilities.MagPlanningZien"/>; muteren onder
/// <see cref="Capabilities.MagKinderenBeheren"/> ("Kindgegevens en plaatsing beheren").
/// </summary>
[ApiController]
[Route("api/dagplaatsingen")]
[Authorize(Policy = Capabilities.MagPlanningZien)]
public sealed class DagplaatsingenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;

    public DagplaatsingenController(KinderKompasDbContext db)
    {
        _db = db;
    }

    /// <summary>De dagafwijkingen binnen een datumbereik, optioneel voor één kind.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DagplaatsingDto>>> Lijst(
        [FromQuery] DateOnly van, [FromQuery] DateOnly tot, [FromQuery] Guid? kindId, CancellationToken ct)
    {
        if (tot < van)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ongeldig bereik",
                Detail = "De einddatum mag niet vóór de begindatum liggen.",
            });
        }

        IQueryable<Dagplaatsing> query = _db.Dagplaatsingen.AsNoTracking()
            .Include(d => d.Kind)
            .Include(d => d.Stamgroep)
            .Where(d => d.Datum >= van && d.Datum <= tot);
        if (kindId is { } kid)
        {
            query = query.Where(d => d.KindId == kid);
        }

        var afwijkingen = await query.OrderBy(d => d.Datum).ToListAsync(ct);
        IReadOnlyList<DagplaatsingDto> resultaat = afwijkingen
            .Select(d => DagplaatsingMapper.NaarDto(d, d.Kind!, d.Stamgroep?.Naam))
            .ToList();
        return Ok(resultaat);
    }

    /// <summary>
    /// Zet een dagafwijking voor (kind, datum). Bestaat er al één op die dag, dan wordt
    /// die overschreven (upsert) — er is hooguit één afwijking per kind per dag.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<ActionResult<DagplaatsingDto>> Zetten(DagplaatsingInvoer invoer, CancellationToken ct)
    {
        Kind? kind = await _db.Kinderen.FirstOrDefaultAsync(k => k.Id == invoer.KindId, ct);
        if (kind is null)
        {
            return BadRequest(new ProblemDetails { Title = "Onbekend kind", Detail = "Het kind bestaat niet." });
        }

        Stamgroep? stamgroep = null;
        if (invoer.StamgroepId is { } sid)
        {
            stamgroep = await _db.Stamgroepen.FirstOrDefaultAsync(s => s.Id == sid, ct);
            if (stamgroep is null)
            {
                return BadRequest(new ProblemDetails { Title = "Onbekende stamgroep", Detail = "De stamgroep bestaat niet." });
            }
        }

        // Upsert op de unieke sleutel (kind, datum): bestaande afwijking bijwerken of nieuw maken.
        Dagplaatsing? bestaand = await _db.Dagplaatsingen
            .FirstOrDefaultAsync(d => d.KindId == invoer.KindId && d.Datum == invoer.Datum, ct);
        if (bestaand is null)
        {
            bestaand = new Dagplaatsing { KindId = invoer.KindId, Datum = invoer.Datum };
            _db.Dagplaatsingen.Add(bestaand);
        }

        bestaand.StamgroepId = invoer.StamgroepId;
        bestaand.Soort = invoer.Soort;
        bestaand.Notitie = string.IsNullOrWhiteSpace(invoer.Notitie) ? null : invoer.Notitie.Trim();
        await _db.SaveChangesAsync(ct);

        return Ok(DagplaatsingMapper.NaarDto(bestaand, kind, stamgroep?.Naam));
    }

    /// <summary>Verwijdert een dagafwijking (het kind valt weer terug op zijn reguliere patroon).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        Dagplaatsing? afwijking = await _db.Dagplaatsingen.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (afwijking is null)
        {
            return NotFound();
        }

        _db.Dagplaatsingen.Remove(afwijking);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
