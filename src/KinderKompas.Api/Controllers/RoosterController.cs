using KinderKompas.Application.Planning;
using KinderKompas.Application.Rooster;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Het werkrooster (fase 5c): een auto-rooster-voorstel genereren, het rooster met
/// BKR-indicatoren en kleurcodering ophalen, diensten bijwerken (taak + urencorrectie)
/// en het rooster definitief versturen. Genereren/bewerken vereist
/// <see cref="Capabilities.MagRoosterBeheren"/>; versturen
/// <see cref="Capabilities.MagRoosterVersturen"/>.
/// </summary>
[ApiController]
[Route("api/rooster")]
public sealed class RoosterController : ControllerBase
{
    private readonly KinderKompasDbContext _db;

    public RoosterController(KinderKompasDbContext db)
    {
        _db = db;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Het weekrooster voor de week waarin <paramref name="datum"/> valt (default: deze week).</summary>
    [HttpGet]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<ActionResult<RoosterWeekDto>> Week([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(datum ?? Vandaag);
        RoosterWeekDto dto = await BouwWeekDto(weekBegin, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Genereert een auto-rooster-voorstel voor de week en slaat het op als concept.
    /// Bestaande diensten van die week worden vervangen; de status gaat terug naar concept.
    /// </summary>
    [HttpPost("genereer")]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<ActionResult<RoosterWeekDto>> Genereer([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(datum ?? Vandaag);
        DateOnly weekEinde = weekBegin.AddDays(6);

        WeekplanningDto weekplanning = await LaadWeekplanning(weekBegin, ct);
        IReadOnlyList<GroepDagBehoefte> behoeften = RoosterBouwer.BehoeftenUit(weekplanning);

        List<Medewerker> medewerkers = await _db.Medewerkers.ToListAsync(ct);
        List<Verlofaanvraag> verlof = await _db.Verlofaanvragen
            .Where(v => v.Status == VerlofStatus.Goedgekeurd && v.Begindatum <= weekEinde && v.Einddatum >= weekBegin)
            .ToListAsync(ct);
        List<Ziekmelding> ziek = await _db.Ziekmeldingen
            .Where(z => z.Begindatum <= weekEinde && (z.Einddatum == null || z.Einddatum >= weekBegin))
            .ToListAsync(ct);

        IReadOnlyList<RoosterVoorstelRegel> regels =
            RoosterGenerator.GenereerVoorstel(medewerkers, behoeften, verlof, ziek);

        Roosterweek week = await VindOfMaakWeek(weekBegin, ct);

        // Bestaande diensten van deze week vervangen; terug naar concept.
        List<Roosterdienst> bestaand = await _db.Roosterdiensten
            .Where(d => d.RoosterweekId == week.Id).ToListAsync(ct);
        _db.Roosterdiensten.RemoveRange(bestaand);

        foreach (RoosterVoorstelRegel regel in regels)
        {
            _db.Roosterdiensten.Add(new Roosterdienst
            {
                RoosterweekId = week.Id,
                MedewerkerId = regel.MedewerkerId,
                StamgroepId = regel.StamgroepId,
                Datum = regel.Datum,
            });
        }

        week.Status = RoosterStatus.Concept;
        week.VerstuurdOp = null;
        await _db.SaveChangesAsync(ct);

        return Ok(await BouwWeekDto(weekBegin, ct));
    }

    /// <summary>Het rooster van een week definitief versturen (zichtbaar voor medewerkers).</summary>
    [HttpPost("{id:guid}/versturen")]
    [Authorize(Policy = Capabilities.MagRoosterVersturen)]
    public async Task<ActionResult<RoosterWeekDto>> Versturen(Guid id, CancellationToken ct)
    {
        Roosterweek? week = await _db.Roosterweken.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (week is null)
        {
            return NotFound();
        }

        week.Status = RoosterStatus.Verstuurd;
        week.VerstuurdOp = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(await BouwWeekDto(week.WeekBegin, ct));
    }

    /// <summary>
    /// Een verstuurd rooster herroepen: terug naar concept zodat het aangepast en
    /// opnieuw verstuurd kan worden. Alleen toegestaan op een verstuurd rooster.
    /// </summary>
    [HttpPost("{id:guid}/herroepen")]
    [Authorize(Policy = Capabilities.MagRoosterVersturen)]
    public async Task<ActionResult<RoosterWeekDto>> Herroepen(Guid id, CancellationToken ct)
    {
        Roosterweek? week = await _db.Roosterweken.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (week is null)
        {
            return NotFound();
        }
        if (week.Status != RoosterStatus.Verstuurd)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Niet verstuurd",
                Detail = "Alleen een verstuurd rooster kan worden herroepen.",
            });
        }

        week.Status = RoosterStatus.Concept;
        week.VerstuurdOp = null;
        await _db.SaveChangesAsync(ct);

        return Ok(await BouwWeekDto(week.WeekBegin, ct));
    }

    /// <summary>
    /// Log van verstuurde roosters, nieuwste eerst en filterbaar op een datumbereik
    /// (de client biedt presets week/maand/kwartaal/jaar via <paramref name="van"/>/<paramref name="tot"/>).
    /// </summary>
    [HttpGet("verstuurd")]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<ActionResult<IReadOnlyList<VerstuurdRoosterDto>>> VerstuurdeRoosters(
        [FromQuery] DateOnly? van, [FromQuery] DateOnly? tot, CancellationToken ct)
    {
        IQueryable<Roosterweek> query = _db.Roosterweken.AsNoTracking()
            .Where(w => w.Status == RoosterStatus.Verstuurd);
        if (van is { } v)
        {
            query = query.Where(w => w.WeekBegin >= v);
        }
        if (tot is { } t)
        {
            query = query.Where(w => w.WeekBegin <= t);
        }

        List<VerstuurdRoosterDto> lijst = await query
            .OrderByDescending(w => w.WeekBegin)
            .Select(w => new VerstuurdRoosterDto(w.Id, w.WeekBegin, w.VerstuurdOp, w.Diensten.Count))
            .ToListAsync(ct);
        return Ok(lijst);
    }

    /// <summary>Een dienst bijwerken: taakomschrijving en urencorrectie (in kwartieren).</summary>
    [HttpPut("dienst/{id:guid}")]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<IActionResult> DienstBijwerken(Guid id, DienstInvoer invoer, CancellationToken ct)
    {
        Roosterdienst? dienst = await _db.Roosterdiensten.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dienst is null)
        {
            return NotFound();
        }

        dienst.Taakomschrijving = invoer.Taakomschrijving;
        dienst.UrencorrectieKwartieren = invoer.UrencorrectieKwartieren;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Handmatig een dienst toevoegen aan de roosterweek van de gegeven datum.</summary>
    [HttpPost("dienst")]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<IActionResult> DienstToevoegen(DienstToevoegenInvoer invoer, CancellationToken ct)
    {
        if (Aanwezigheid.NaarWeekdag(invoer.Datum) == Weekdag.Geen)
        {
            return BadRequest(new ProblemDetails { Title = "Geen opvangdag", Detail = "Een dienst kan alleen op ma t/m vr." });
        }
        if (!await _db.Medewerkers.AnyAsync(m => m.Id == invoer.MedewerkerId, ct))
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende medewerker", Detail = "De medewerker bestaat niet." });
        }
        if (!await _db.Stamgroepen.AnyAsync(s => s.Id == invoer.StamgroepId, ct))
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende stamgroep", Detail = "De stamgroep bestaat niet." });
        }

        bool bestaatAl = await _db.Roosterdiensten.AnyAsync(
            d => d.MedewerkerId == invoer.MedewerkerId && d.Datum == invoer.Datum && d.StamgroepId == invoer.StamgroepId, ct);
        if (bestaatAl)
        {
            return Conflict(new ProblemDetails { Title = "Dienst bestaat al", Detail = "Deze medewerker staat al in deze groep op deze dag." });
        }

        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(invoer.Datum);
        Roosterweek week = await VindOfMaakWeek(weekBegin, ct);

        _db.Roosterdiensten.Add(new Roosterdienst
        {
            RoosterweekId = week.Id,
            MedewerkerId = invoer.MedewerkerId,
            StamgroepId = invoer.StamgroepId,
            Datum = invoer.Datum,
        });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Een dienst uit het rooster verwijderen.</summary>
    [HttpDelete("dienst/{id:guid}")]
    [Authorize(Policy = Capabilities.MagRoosterBeheren)]
    public async Task<IActionResult> DienstVerwijderen(Guid id, CancellationToken ct)
    {
        Roosterdienst? dienst = await _db.Roosterdiensten.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dienst is null)
        {
            return NotFound();
        }

        _db.Roosterdiensten.Remove(dienst);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // === Privé hulpfuncties ===

    private async Task<WeekplanningDto> LaadWeekplanning(DateOnly weekBegin, CancellationToken ct)
    {
        var stamgroepen = await _db.Stamgroepen
            .AsNoTracking()
            .Include(s => s.Kinderen)
            .OrderBy(s => s.Naam)
            .ToListAsync(ct);
        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        return WeekplanningBouwer.Bouw(weekBegin, stamgroepen, vakanties);
    }

    private async Task<RoosterWeekDto> BouwWeekDto(DateOnly weekBegin, CancellationToken ct)
    {
        DateOnly weekEinde = weekBegin.AddDays(6);
        WeekplanningDto weekplanning = await LaadWeekplanning(weekBegin, ct);

        Roosterweek? week = await _db.Roosterweken
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WeekBegin == weekBegin, ct);

        List<Roosterdienst> diensten = week is null
            ? new List<Roosterdienst>()
            : await _db.Roosterdiensten.AsNoTracking()
                .Where(d => d.RoosterweekId == week.Id).ToListAsync(ct);

        List<Medewerker> medewerkers = await _db.Medewerkers.AsNoTracking().ToListAsync(ct);
        List<Verlofaanvraag> verlof = await _db.Verlofaanvragen.AsNoTracking()
            .Where(v => v.Status != VerlofStatus.Afgekeurd && v.Begindatum <= weekEinde && v.Einddatum >= weekBegin)
            .ToListAsync(ct);
        List<Ziekmelding> ziek = await _db.Ziekmeldingen.AsNoTracking()
            .Where(z => z.Begindatum <= weekEinde && (z.Einddatum == null || z.Einddatum >= weekBegin))
            .ToListAsync(ct);

        return RoosterBouwer.Bouw(weekplanning, week, diensten, medewerkers, verlof, ziek);
    }

    private async Task<Roosterweek> VindOfMaakWeek(DateOnly weekBegin, CancellationToken ct)
    {
        Roosterweek? week = await _db.Roosterweken.FirstOrDefaultAsync(w => w.WeekBegin == weekBegin, ct);
        if (week is null)
        {
            week = new Roosterweek { WeekBegin = weekBegin, Status = RoosterStatus.Concept };
            _db.Roosterweken.Add(week);
            await _db.SaveChangesAsync(ct);
        }

        return week;
    }
}
