using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Application.Planning;
using KinderKompas.Application.Portaal;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Het Groepsportaal (fase 8): de gedeelde tablet-context op locatie. Toont de
/// dienst van de dag, laat een medewerker zichzelf in-/uitklokken (urenregistratie
/// in kwartieren) en geeft — als enige medewerker-context — toegang tot de
/// oudergegevens van de kinderen op locatie.
///
/// Afgeschermd met <see cref="Capabilities.MagGroepsportaalGebruiken"/>. Observaties
/// uploaden/versturen loopt via de bestaande ObservatiesController, die het portaal
/// nu als "ziet alle kinderen" behandelt. Wachtlijst, contacten, rondleidingen,
/// instellingen en medewerkersbeheer zijn hier bewust NIET bereikbaar (geen capability).
/// </summary>
[ApiController]
[Route("api/groepsportaal")]
[Authorize(Policy = Capabilities.MagGroepsportaalGebruiken)]
public sealed class GroepsportaalController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly ICurrentUser _huidigeGebruiker;

    public GroepsportaalController(KinderKompasDbContext db, ICurrentUser huidigeGebruiker)
    {
        _db = db;
        _huidigeGebruiker = huidigeGebruiker;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Naam(Medewerker? m) => m is null ? "" : $"{m.Voornaam} {m.Achternaam}";

    // ---- Dienst van de dag ---------------------------------------------------

    /// <summary>De ingeplande diensten van een dag op locatie (default: vandaag).</summary>
    [HttpGet("dienst")]
    public async Task<ActionResult<GroepsportaalDagDto>> Dienst([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly dag = datum ?? Vandaag;
        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(dag);

        Roosterweek? week = await _db.Roosterweken.AsNoTracking()
            .FirstOrDefaultAsync(w => w.WeekBegin == weekBegin, ct);

        List<Roosterdienst> diensten = await _db.Roosterdiensten.AsNoTracking()
            .Include(d => d.Medewerker)
            .Include(d => d.Stamgroep)
            .Where(d => d.Datum == dag)
            .OrderBy(d => d.Stamgroep!.Naam).ThenBy(d => d.Medewerker!.Achternaam)
            .ToListAsync(ct);

        List<DagdienstDto> regels = diensten.Select(d => new DagdienstDto(
            d.Id,
            d.MedewerkerId,
            Naam(d.Medewerker),
            d.StamgroepId,
            d.Stamgroep?.Naam ?? "",
            d.Taakomschrijving)).ToList();

        return Ok(new GroepsportaalDagDto(dag, week?.IsVerstuurd ?? false, regels));
    }

    /// <summary>De medewerkers om uit te kiezen bij het inklokken (alleen id + naam).</summary>
    [HttpGet("medewerkers")]
    public async Task<ActionResult<IReadOnlyList<PortaalMedewerkerDto>>> Medewerkers(CancellationToken ct)
    {
        var medewerkers = await _db.Medewerkers.AsNoTracking()
            .OrderBy(m => m.Achternaam).ThenBy(m => m.Voornaam)
            .Select(m => new PortaalMedewerkerDto(m.Id, m.Voornaam + " " + m.Achternaam))
            .ToListAsync(ct);
        return Ok(medewerkers);
    }

    // ---- Kinderen + oudergegevens (alleen hier) ------------------------------

    /// <summary>De kinderen op locatie, mét oudergegevens (portaal heeft die capability).</summary>
    [HttpGet("kinderen")]
    public async Task<ActionResult<IReadOnlyList<KindDto>>> Kinderen(
        [FromQuery] Guid? stamgroepId, CancellationToken ct)
    {
        IQueryable<Kind> query = _db.Kinderen.AsNoTracking();
        if (stamgroepId is { } gid)
        {
            query = query.Where(k => k.StamgroepId == gid);
        }

        List<Kind> kinderen = await query
            .OrderBy(k => k.Achternaam).ThenBy(k => k.Voornaam)
            .ToListAsync(ct);

        IReadOnlyList<KindDto> resultaat = kinderen
            .Select(k => KindMapper.NaarDto(k, _huidigeGebruiker, Vandaag))
            .ToList();
        return Ok(resultaat);
    }

    // ---- In-/uitklokken (urenregistratie in kwartieren) ----------------------

    /// <summary>Inklokken: de medewerker kiest zichzelf op het tablet.</summary>
    [HttpPost("inklokken")]
    public async Task<ActionResult<UrenregistratieDto>> Inklokken(InklokInvoer invoer, CancellationToken ct)
    {
        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == invoer.MedewerkerId, ct);
        if (medewerker is null)
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende medewerker", Detail = "De medewerker bestaat niet." });
        }

        if (invoer.StamgroepId is { } sid && !await _db.Stamgroepen.AnyAsync(s => s.Id == sid, ct))
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende stamgroep", Detail = "De stamgroep bestaat niet." });
        }
        if (invoer.RoosterdienstId is { } did && !await _db.Roosterdiensten.AnyAsync(d => d.Id == did, ct))
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende dienst", Detail = "De dienst bestaat niet." });
        }

        DateOnly dag = Vandaag;
        bool alIngeklokt = await _db.Urenregistraties
            .AnyAsync(u => u.MedewerkerId == invoer.MedewerkerId && u.Datum == dag && u.Uitgeklokt == null, ct);
        if (alIngeklokt)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Al ingeklokt",
                Detail = "Deze medewerker is vandaag nog ingeklokt. Klok eerst uit.",
            });
        }

        var registratie = new Urenregistratie
        {
            MedewerkerId = invoer.MedewerkerId,
            RoosterdienstId = invoer.RoosterdienstId,
            StamgroepId = invoer.StamgroepId,
            Datum = dag,
            Ingeklokt = DateTime.UtcNow,
        };
        _db.Urenregistraties.Add(registratie);
        await _db.SaveChangesAsync(ct);

        string? groepNaam = await GroepNaam(registratie.StamgroepId, ct);
        return Ok(UrenregistratieMapper.NaarDto(registratie, Naam(medewerker), groepNaam));
    }

    /// <summary>Uitklokken: legt de werkelijk gewerkte tijd (kwartieren) vast.</summary>
    [HttpPost("uitklokken/{id:guid}")]
    public async Task<ActionResult<UrenregistratieDto>> Uitklokken(Guid id, CancellationToken ct)
    {
        Urenregistratie? registratie = await _db.Urenregistraties
            .Include(u => u.Medewerker)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (registratie is null)
        {
            return NotFound();
        }
        if (!registratie.IsOpen)
        {
            return Conflict(new ProblemDetails { Title = "Al uitgeklokt", Detail = "Deze registratie is al afgesloten." });
        }

        registratie.Uitgeklokt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        string? groepNaam = await GroepNaam(registratie.StamgroepId, ct);
        return Ok(UrenregistratieMapper.NaarDto(registratie, Naam(registratie.Medewerker), groepNaam));
    }

    /// <summary>De urenregistraties van een dag (default: vandaag) — wie is ingeklokt.</summary>
    [HttpGet("urenregistratie")]
    public async Task<ActionResult<IReadOnlyList<UrenregistratieDto>>> Urenregistratie(
        [FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly dag = datum ?? Vandaag;
        var registraties = await _db.Urenregistraties.AsNoTracking()
            .Include(u => u.Medewerker)
            .Include(u => u.Stamgroep)
            .Where(u => u.Datum == dag)
            .OrderBy(u => u.Ingeklokt)
            .ToListAsync(ct);

        IReadOnlyList<UrenregistratieDto> resultaat = registraties
            .Select(u => UrenregistratieMapper.NaarDto(u, Naam(u.Medewerker), u.Stamgroep?.Naam))
            .ToList();
        return Ok(resultaat);
    }

    private async Task<string?> GroepNaam(Guid? stamgroepId, CancellationToken ct)
    {
        if (stamgroepId is not { } sid)
        {
            return null;
        }
        return await _db.Stamgroepen.AsNoTracking()
            .Where(s => s.Id == sid).Select(s => s.Naam).FirstOrDefaultAsync(ct);
    }
}
