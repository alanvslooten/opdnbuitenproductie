using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Meldingen;
using KinderKompas.Application.Wachtlijst;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Meldingen;
using KinderKompas.Domain.ValueObjects;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>Toggle om een inschrijving handmatig bovenaan te zetten (bijv. een personeelskind).</summary>
public sealed record HandmatigBovenaanInvoer(bool Bovenaan);

/// <summary>
/// Wachtlijst &amp; plaatsing. Afgeschermd met <see cref="Capabilities.MagWachtlijstBeheren"/>.
/// Alle queries lopen via de tenant-gefilterde DbContext (enkel de eigen organisatie).
/// De prioriteitsscore en de voorstel-BKR-impact komen volledig uit het domein; de
/// controller laadt data, valideert de plaatsingsregels tegen de database en mapt.
/// </summary>
[ApiController]
[Route("api/wachtlijst")]
[Authorize(Policy = Capabilities.MagWachtlijstBeheren)]
public sealed class WachtlijstController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly ICurrentUser _huidigeGebruiker;
    private readonly IValidator<WachtlijstInvoer> _validator;
    private readonly IValidator<VoorstelInvoer> _voorstelValidator;
    private readonly IPlaatsingsToDo _plaatsingsToDo;
    private readonly IMeldingDispatcher _meldingen;
    private readonly IInstellingenProvider _instellingen;

    public WachtlijstController(
        KinderKompasDbContext db,
        ICurrentUser huidigeGebruiker,
        IValidator<WachtlijstInvoer> validator,
        IValidator<VoorstelInvoer> voorstelValidator,
        IPlaatsingsToDo plaatsingsToDo,
        IMeldingDispatcher meldingen,
        IInstellingenProvider instellingen)
    {
        _db = db;
        _huidigeGebruiker = huidigeGebruiker;
        _validator = validator;
        _voorstelValidator = voorstelValidator;
        _plaatsingsToDo = plaatsingsToDo;
        _meldingen = meldingen;
        _instellingen = instellingen;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Bouwt de weergavecontext (prioriteitsgewichten + 'binnenkort 4'-drempel) uit de instellingen.</summary>
    private async Task<WachtlijstWeergaveContext> WeergaveContextAsync(CancellationToken ct)
    {
        OrganisatieInstellingen inst = await _instellingen.HuidigeAsync(ct);
        return new WachtlijstWeergaveContext(
            new WachtlijstPrioriteitsgewichten(inst.PrioriteitInternGewicht, inst.PrioriteitPerMaandGewicht),
            inst.KindBinnenkortVierDrempelDagen);
    }

    // === Wachtlijst-CRUD ===

    /// <summary>De wachtlijst, gesorteerd op prioriteit (handmatig bovenaan, dan score, dan langst wachtend).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WachtlijstInschrijvingDto>>> Lijst(
        [FromQuery] bool toonGeplaatst, CancellationToken ct)
    {
        IQueryable<WachtlijstInschrijving> query =
            _db.Wachtlijstinschrijvingen.AsNoTracking().Include(w => w.Voorstellen);
        if (!toonGeplaatst)
        {
            query = query.Where(w => w.Status == WachtlijstStatus.Wachtend);
        }

        List<WachtlijstInschrijving> inschrijvingen = await query.ToListAsync(ct);
        WachtlijstWeergaveContext context = await WeergaveContextAsync(ct);

        IEnumerable<WachtlijstInschrijvingDto> dtos =
            inschrijvingen.Select(w => WachtlijstMapper.NaarDto(w, _huidigeGebruiker, Vandaag, context));

        return Ok(WachtlijstSortering.OpPrioriteit(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WachtlijstInschrijvingDto>> Detail(Guid id, CancellationToken ct)
    {
        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);

        return inschrijving is null
            ? NotFound()
            : Ok(WachtlijstMapper.NaarDto(inschrijving, _huidigeGebruiker, Vandaag, await WeergaveContextAsync(ct)));
    }

    [HttpPost]
    public async Task<ActionResult<WachtlijstInschrijvingDto>> Aanmaken(WachtlijstInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        if (await ControleerGewensteStamgroep(invoer.GewensteStamgroepId, ct) is { } groepFout)
        {
            return groepFout;
        }

        var inschrijving = new WachtlijstInschrijving { Voornaam = invoer.Voornaam, Achternaam = invoer.Achternaam };
        WachtlijstMapper.PasInvoerToe(inschrijving, invoer);

        _db.Wachtlijstinschrijvingen.Add(inschrijving);
        await _db.SaveChangesAsync(ct);

        // Actiecentrum: een nieuwe aanmelding melden (informatief).
        await _meldingen.PubliceerAsync(
            new NieuweWachtlijstaanmelding(inschrijving.Id, $"{inschrijving.Voornaam} {inschrijving.Achternaam}"), ct);

        WachtlijstInschrijvingDto dto = WachtlijstMapper.NaarDto(inschrijving, _huidigeGebruiker, Vandaag, await WeergaveContextAsync(ct));
        return CreatedAtAction(nameof(Detail), new { id = inschrijving.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WachtlijstInschrijvingDto>> Bewerken(
        Guid id, WachtlijstInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        if (await ControleerGewensteStamgroep(invoer.GewensteStamgroepId, ct) is { } groepFout)
        {
            return groepFout;
        }

        WachtlijstMapper.PasInvoerToe(inschrijving, invoer);
        await _db.SaveChangesAsync(ct);

        return Ok(WachtlijstMapper.NaarDto(inschrijving, _huidigeGebruiker, Vandaag, await WeergaveContextAsync(ct)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        _db.Wachtlijstinschrijvingen.Remove(inschrijving); // voorstellen cascaden mee
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Zet een inschrijving handmatig bovenaan of haalt dat weg (personeelskind).</summary>
    [HttpPost("{id:guid}/bovenaan")]
    public async Task<ActionResult<WachtlijstInschrijvingDto>> ZetBovenaan(
        Guid id, HandmatigBovenaanInvoer invoer, CancellationToken ct)
    {
        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        inschrijving.HandmatigBovenaan = invoer.Bovenaan;
        await _db.SaveChangesAsync(ct);
        return Ok(WachtlijstMapper.NaarDto(inschrijving, _huidigeGebruiker, Vandaag, await WeergaveContextAsync(ct)));
    }

    // === Voorstel-flow ===

    /// <summary>
    /// De controle-analyse voor de voorstel-pop-up: per openstaande dag de bezetting,
    /// de BKR-impact (huidig + mét dit kind), plek-beschikbaarheid en de groepsgrootte-
    /// check. <paramref name="stamgroepId"/> is optioneel (default: de gewenste groep),
    /// <paramref name="startdatum"/> ook (default: de gewenste startdatum).
    /// </summary>
    [HttpGet("{id:guid}/voorstel-analyse")]
    public async Task<ActionResult<VoorstelAnalyseDto>> VoorstelAnalyse(
        Guid id, [FromQuery] Guid? stamgroepId, [FromQuery] DateOnly? startdatum, CancellationToken ct)
    {
        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        Guid? doelGroepId = stamgroepId ?? inschrijving.GewensteStamgroepId;
        if (doelGroepId is not { } groepId)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Geen stamgroep",
                Detail = "Kies een stamgroep om de plaatsing tegen te analyseren; deze inschrijving heeft geen voorkeursgroep.",
            });
        }

        Stamgroep? doelStamgroep = await _db.Stamgroepen
            .AsNoTracking()
            .Include(s => s.Kinderen)
            .FirstOrDefaultAsync(s => s.Id == groepId, ct);
        if (doelStamgroep is null)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Onbekende stamgroep",
                Detail = "De opgegeven stamgroep bestaat niet binnen deze organisatie.",
            });
        }

        var vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);

        // Nog OPENSTAANDE voorstellen voor deze groep (van andere inschrijvingen) tellen
        // mee als voorlopige bezetting, zodat twee tegelijk lopende voorstellen niet samen
        // ongemerkt de BKR overschrijden. Elk wordt een transiënt kind op zijn voorgestelde
        // dagen vanaf de vroegste voorgestelde datum.
        var openVoorstellen = await _db.Voorstellen.AsNoTracking()
            .Include(v => v.Dagen)
            .Include(v => v.WachtlijstInschrijving)
            .Where(v => v.VoorgesteldeStamgroepId == groepId
                && v.Status == VoorstelStatus.Verstuurd
                && v.WachtlijstInschrijvingId != id)
            .ToListAsync(ct);

        var openVoorstelKinderen = openVoorstellen
            .Where(v => v.WachtlijstInschrijving is not null && v.Dagen.Count > 0)
            .Select(v => new Kind
            {
                Voornaam = "Voorstel",
                Achternaam = "(openstaand)",
                Geboortedatum = v.WachtlijstInschrijving!.Geboortedatum,
                Contracttype = v.WachtlijstInschrijving.Contracttype,
                GewensteOpvangdagen = v.VoorgesteldeDagen,
                Startdatum = v.Dagen.Min(d => d.VoorgesteldeDatum),
                StamgroepId = groepId,
            })
            .ToList();

        VoorstelAnalyseDto analyse = VoorstelAnalyseBouwer.Bouw(
            inschrijving, doelStamgroep, vakanties, startdatum, openVoorstelKinderen);
        return Ok(analyse);
    }

    /// <summary>De voorstelhistorie van een inschrijving (nieuwste eerst), inclusief deelvoorstellen.</summary>
    [HttpGet("{id:guid}/voorstellen")]
    public async Task<ActionResult<IReadOnlyList<VoorstelDto>>> Voorstelhistorie(Guid id, CancellationToken ct)
    {
        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .AsNoTracking()
            .Include(w => w.Voorstellen).ThenInclude(v => v.Dagen)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        Dictionary<Guid, string> groepNamen = await _db.Stamgroepen.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Naam, ct);

        IReadOnlyList<VoorstelDto> historie = inschrijving.Voorstellen
            .OrderByDescending(v => v.VerstuurdOp)
            .Select(v => VoorstelMapper.NaarDto(
                v, inschrijving.GewensteOpvangdagen, groepNamen.GetValueOrDefault(v.VoorgesteldeStamgroepId)))
            .ToList();

        return Ok(historie);
    }

    /// <summary>
    /// Verstuurt een (deel)voorstel voor een subset van de openstaande gewenste dagen.
    /// De voorgestelde dagen moeten openstaan; de resterende dagen blijven op de wachtlijst.
    /// </summary>
    [HttpPost("{id:guid}/voorstellen")]
    public async Task<ActionResult<VoorstelDto>> VerstuurVoorstel(Guid id, VoorstelInvoer invoer, CancellationToken ct)
    {
        if (await _voorstelValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        WachtlijstInschrijving? inschrijving = await _db.Wachtlijstinschrijvingen
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (inschrijving is null)
        {
            return NotFound();
        }

        // De voorgestelde dagen moeten allemaal nog openstaan.
        if ((invoer.Dagen & ~inschrijving.OpenstaandeDagen) != Weekdag.Geen)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Dagen niet (meer) open",
                Detail = "Een of meer voorgestelde dagen staan niet meer open op de wachtlijst.",
            });
        }

        // Elke voorgestelde dag moet één concrete datum hebben en omgekeerd.
        if (!DagDataDektPrecies(invoer))
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Onvolledige dagdata",
                Detail = "Geef voor elke voorgestelde dag precies één startdatum op.",
            });
        }

        Stamgroep? stamgroep = await _db.Stamgroepen.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == invoer.StamgroepId, ct);
        if (stamgroep is null)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Onbekende stamgroep",
                Detail = "De opgegeven stamgroep bestaat niet binnen deze organisatie.",
            });
        }

        var voorstel = new Voorstel
        {
            WachtlijstInschrijvingId = inschrijving.Id,
            VerstuurdOp = DateTime.UtcNow,
            VoorgesteldeStamgroepId = invoer.StamgroepId,
            VoorgesteldeDagen = invoer.Dagen,
            Status = VoorstelStatus.Verstuurd,
            Notitie = invoer.Notitie,
            Dagen = invoer.DagData
                .Select(d => new VoorstelDag { Weekdag = d.Weekdag, VoorgesteldeDatum = d.VoorgesteldeDatum })
                .ToList(),
        };

        _db.Voorstellen.Add(voorstel);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Voorstelhistorie), new { id = inschrijving.Id },
            VoorstelMapper.NaarDto(voorstel, inschrijving.GewensteOpvangdagen));
    }

    /// <summary>
    /// Accepteert een voorstel: de voorgestelde dagen worden geplaatst en — als alle
    /// gewenste dagen daarmee gedekt zijn — de inschrijving op Geplaatst gezet. Triggert
    /// de contract-to-do voor Gail (Portabase). Resterende dagen blijven wachtend.
    /// </summary>
    [HttpPost("voorstellen/{voorstelId:guid}/accepteren")]
    public async Task<ActionResult<WachtlijstInschrijvingDto>> AccepteerVoorstel(Guid voorstelId, CancellationToken ct)
    {
        Voorstel? voorstel = await _db.Voorstellen
            .Include(v => v.WachtlijstInschrijving)
            .FirstOrDefaultAsync(v => v.Id == voorstelId, ct);
        if (voorstel is null || voorstel.WachtlijstInschrijving is null)
        {
            return NotFound();
        }

        if (voorstel.Status != VoorstelStatus.Verstuurd)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Voorstel al beantwoord",
                Detail = $"Dit voorstel is al {voorstel.Status.ToString().ToLowerInvariant()}.",
            });
        }

        WachtlijstInschrijving inschrijving = voorstel.WachtlijstInschrijving;

        voorstel.Status = VoorstelStatus.Geaccepteerd;
        voorstel.BeantwoordOp = DateTime.UtcNow;
        inschrijving.VerwerkGeaccepteerdVoorstel(voorstel.VoorgesteldeDagen);

        await _db.SaveChangesAsync(ct);

        // Trigger-punt: contract opmaken in Portabase (fase 9 maakt hier een echte to-do van).
        DateOnly start = voorstel.Dagen.Count > 0
            ? voorstel.Dagen.Min(d => d.VoorgesteldeDatum)
            : inschrijving.GewensteStartdatum;
        await _plaatsingsToDo.ContractOpmakenAsync(
            new PlaatsingVoltooidGebeurtenis(
                inschrijving.Id,
                $"{inschrijving.Voornaam} {inschrijving.Achternaam}",
                voorstel.VoorgesteldeStamgroepId,
                voorstel.VoorgesteldeDagen,
                start,
                inschrijving.IsVolledigGeplaatst),
            ct);

        return Ok(WachtlijstMapper.NaarDto(inschrijving, _huidigeGebruiker, Vandaag, await WeergaveContextAsync(ct)));
    }

    /// <summary>Wijst een voorstel af: de dagen blijven op de wachtlijst staan.</summary>
    [HttpPost("voorstellen/{voorstelId:guid}/afwijzen")]
    public async Task<ActionResult<VoorstelDto>> WijsVoorstelAf(Guid voorstelId, CancellationToken ct)
    {
        Voorstel? voorstel = await _db.Voorstellen
            .Include(v => v.WachtlijstInschrijving)
            .Include(v => v.Dagen)
            .FirstOrDefaultAsync(v => v.Id == voorstelId, ct);
        if (voorstel is null || voorstel.WachtlijstInschrijving is null)
        {
            return NotFound();
        }

        if (voorstel.Status != VoorstelStatus.Verstuurd)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Voorstel al beantwoord",
                Detail = $"Dit voorstel is al {voorstel.Status.ToString().ToLowerInvariant()}.",
            });
        }

        voorstel.Status = VoorstelStatus.Afgewezen;
        voorstel.BeantwoordOp = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(VoorstelMapper.NaarDto(voorstel, voorstel.WachtlijstInschrijving.GewensteOpvangdagen));
    }

    // === Hulpfuncties ===

    private async Task<ActionResult?> ControleerGewensteStamgroep(Guid? stamgroepId, CancellationToken ct)
    {
        if (stamgroepId is not { } gid)
        {
            return null; // geen voorkeursgroep is toegestaan
        }

        bool bestaat = await _db.Stamgroepen.AsNoTracking().AnyAsync(s => s.Id == gid, ct);
        return bestaat
            ? null
            : UnprocessableEntity(new ProblemDetails
            {
                Title = "Onbekende stamgroep",
                Detail = "De gewenste stamgroep bestaat niet binnen deze organisatie.",
            });
    }

    /// <summary>Of de opgegeven dagdata precies de voorgestelde dagen dekt (elk één datum).</summary>
    private static bool DagDataDektPrecies(VoorstelInvoer invoer)
    {
        Weekdag uitDagData = Weekdag.Geen;
        foreach (VoorstelDagInvoer dag in invoer.DagData)
        {
            if (uitDagData.HasFlag(dag.Weekdag))
            {
                return false; // dubbele dag
            }

            uitDagData |= dag.Weekdag;
        }

        return uitDagData == invoer.Dagen;
    }
}
