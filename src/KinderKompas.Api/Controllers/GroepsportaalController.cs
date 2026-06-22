using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Application.Observaties;
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

    /// <summary>
    /// De stamgroep waartoe dit Groepsportaal-account is beperkt (één tablet per groep).
    /// Null bij een (legacy) ongescoped account — dan wordt er niet op groep gefilterd.
    /// </summary>
    private Guid? GroepId => _huidigeGebruiker.StamgroepId;

    // ---- Dashboard (gescoped op de eigen groep) ------------------------------

    /// <summary>
    /// Beknopt dagdashboard voor de groep-tablet: vandaag aanwezige kinderen + ingeplande
    /// medewerkers, wie er nu ingeklokt is, openstaande observaties en de groepsnaam.
    /// Alles gescoped op de stamgroep van het account.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<GroepsportaalDashboardDto>> Dashboard(
        [FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly dag = datum ?? Vandaag;
        Guid? gid = GroepId;

        List<Stamgroep> groepen = await _db.Stamgroepen.AsNoTracking()
            .Include(s => s.Kinderen)
            .Where(s => gid == null || s.Id == gid)
            .ToListAsync(ct);
        string? naam = groepen.FirstOrDefault()?.Naam;

        List<Schoolvakantie> vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        WeekplanningDto weekplanning = WeekplanningBouwer.Bouw(dag, groepen, vakanties);
        int aanwezig = weekplanning.Stamgroepen
            .SelectMany(s => s.Dagen)
            .Where(d => d.Datum == dag)
            .Sum(d => d.Kinderen.Count);

        int kinderenInGroep = groepen.Sum(s => s.Kinderen.Count);

        int medewerkers = await _db.Roosterdiensten.AsNoTracking()
            .CountAsync(d => d.Datum == dag && (gid == null || d.StamgroepId == gid), ct);

        int ingeklokt = await _db.Urenregistraties.AsNoTracking()
            .CountAsync(u => u.Datum == dag && u.Uitgeklokt == null && (gid == null || u.StamgroepId == gid), ct);

        // Openstaande observaties (overschreden + binnenkort) over de kinderen van de groep.
        int observatiesOpen = 0;
        List<Kind> kinderen = groepen.SelectMany(s => s.Kinderen).ToList();
        if (kinderen.Count > 0)
        {
            List<Guid> ids = kinderen.Select(k => k.Id).ToList();
            List<Observatie> obs = await _db.Observaties.AsNoTracking()
                .Where(o => ids.Contains(o.KindId)).ToListAsync(ct);
            Dictionary<Guid, List<Observatie>> perKind = obs
                .GroupBy(o => o.KindId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (Kind k in kinderen)
            {
                KindObservatieschemaDto schema = ObservatieOverzichtBouwer.Bouw(
                    k, perKind.GetValueOrDefault(k.Id) ?? [], dag);
                observatiesOpen += schema.AantalOverschreden + schema.AantalBinnenkort;
            }
        }

        return Ok(new GroepsportaalDashboardDto(
            dag, naam, kinderenInGroep, aanwezig, medewerkers, ingeklokt, observatiesOpen));
    }

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
            .Where(d => GroepId == null || d.StamgroepId == GroepId)
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
        // Een groepsportaal-account ziet uitsluitend de kinderen van zijn eigen
        // stamgroep; de query-parameter kan dat niet verruimen (alleen verfijnen
        // binnen de eigen groep is zinloos, dus we negeren hem bij een gescoped account).
        Guid? effectieveGroep = GroepId ?? stamgroepId;
        IQueryable<Kind> query = _db.Kinderen.AsNoTracking();
        if (effectieveGroep is { } gid)
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

        // Identiteitscheck: heeft de medewerker een pincode, dan moet die kloppen.
        if (!string.IsNullOrEmpty(medewerker.Pincode) && invoer.Pincode?.Trim() != medewerker.Pincode)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Onjuiste pincode",
                Detail = "Voer je eigen pincode in om in te klokken.",
            });
        }

        // Bij een gescoped account telt de eigen groep; anders de meegegeven groep.
        Guid? stamgroepId = GroepId ?? invoer.StamgroepId;
        if (stamgroepId is { } sid && !await _db.Stamgroepen.AnyAsync(s => s.Id == sid, ct))
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
            StamgroepId = stamgroepId,
            Datum = dag,
            Ingeklokt = DateTime.UtcNow,
        };
        _db.Urenregistraties.Add(registratie);
        await _db.SaveChangesAsync(ct);

        string? groepNaam = await GroepNaam(registratie.StamgroepId, ct);
        return Ok(UrenregistratieMapper.NaarDto(registratie, Naam(medewerker), groepNaam));
    }

    /// <summary>
    /// Uitklokken: legt de werkelijk gewerkte tijd (in kwartieren, ≥7,5 min naar boven)
    /// vast. Optioneel kan een uitkloktijd worden meegegeven — handig als iemand
    /// vergeten is uit te klokken en het achteraf op het juiste tijdstip wil zetten.
    /// Zonder tijd telt "nu".
    /// </summary>
    [HttpPost("uitklokken/{id:guid}")]
    public async Task<ActionResult<UrenregistratieDto>> Uitklokken(
        Guid id, [FromBody] UitklokInvoer? invoer, CancellationToken ct)
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

        DateTime uit = invoer?.Uitgeklokt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (uit <= registratie.Ingeklokt)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Ongeldige uitkloktijd",
                Detail = "De uitkloktijd moet ná de inkloktijd liggen.",
            });
        }

        registratie.Uitgeklokt = uit;
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
            .Where(u => GroepId == null || u.StamgroepId == GroepId)
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

/// <summary>Beknopt dagdashboard voor de groep-tablet (gescoped op de eigen stamgroep).</summary>
public sealed record GroepsportaalDashboardDto(
    DateOnly Datum,
    string? StamgroepNaam,
    int KinderenInGroep,
    int AanwezigVandaag,
    int MedewerkersVandaag,
    int Ingeklokt,
    int ObservatiesOpen);
