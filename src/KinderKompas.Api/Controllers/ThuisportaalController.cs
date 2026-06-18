using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Meldingen;
using KinderKompas.Application.Planning;
using KinderKompas.Application.Portaal;
using KinderKompas.Application.Verlof;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Meldingen;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Het thuis-portaal (fase 8): de persoonlijke medewerker-context. Alles is hard
/// gescoped op de ingelogde medewerker (uit de claim) — een medewerker ziet/bewerkt
/// nooit gegevens van een ander. BEWUST géén oudergegevens, wachtlijst, contacten,
/// observaties-upload of roosters van anderen: die capabilities/projecties ontbreken
/// hier per ontwerp (zie de DTO's in <c>Application.Portaal</c>).
///
/// Afgeschermd met <see cref="Capabilities.MagThuisportaalGebruiken"/>. Het eigen
/// rooster is pas zichtbaar nadat de planner de week heeft verstuurd.
/// </summary>
[ApiController]
[Route("api/thuisportaal")]
[Authorize(Policy = Capabilities.MagThuisportaalGebruiken)]
public sealed class ThuisportaalController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly ICurrentUser _huidigeGebruiker;
    private readonly IValidator<BeschikbaarheidInvoer> _beschikbaarheidValidator;
    private readonly IValidator<VerlofAanvraagInvoer> _verlofValidator;
    private readonly IMeldingDispatcher _meldingen;

    public ThuisportaalController(
        KinderKompasDbContext db,
        ICurrentUser huidigeGebruiker,
        IValidator<BeschikbaarheidInvoer> beschikbaarheidValidator,
        IValidator<VerlofAanvraagInvoer> verlofValidator,
        IMeldingDispatcher meldingen)
    {
        _db = db;
        _huidigeGebruiker = huidigeGebruiker;
        _beschikbaarheidValidator = beschikbaarheidValidator;
        _verlofValidator = verlofValidator;
        _meldingen = meldingen;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    // ---- Eigen rooster (alleen na versturen) ---------------------------------

    /// <summary>Het eigen weekrooster — alleen zichtbaar nadat de planner het heeft verstuurd.</summary>
    [HttpGet("rooster")]
    public async Task<ActionResult<ThuisRoosterDto>> Rooster([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(datum ?? Vandaag);
        Roosterweek? week = await _db.Roosterweken.AsNoTracking()
            .FirstOrDefaultAsync(w => w.WeekBegin == weekBegin, ct);

        List<Roosterdienst> eigenDiensten = week is null
            ? []
            : await _db.Roosterdiensten.AsNoTracking()
                .Where(d => d.RoosterweekId == week.Id && d.MedewerkerId == medewerkerId)
                .ToListAsync(ct);

        Dictionary<Guid, string> groepNamen = await _db.Stamgroepen.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Naam, ct);

        return Ok(ThuisRoosterBouwer.Bouw(weekBegin, week, eigenDiensten, groepNamen));
    }

    // ---- Beschikbaarheid opgeven ---------------------------------------------

    /// <summary>De eigen roosterlagen: vaste werkdagen (door planner) en beschikbaarheid.</summary>
    [HttpGet("beschikbaarheid")]
    public async Task<ActionResult<BeschikbaarheidDto>> Beschikbaarheid(CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        Medewerker? medewerker = await _db.Medewerkers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medewerkerId, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        return Ok(new BeschikbaarheidDto(
            medewerker.Id, medewerker.VasteWerkdagen, medewerker.Beschikbaarheidsdagen));
    }

    /// <summary>De eigen beschikbaarheidsdagen bijwerken (mag niet overlappen met vaste werkdagen).</summary>
    [HttpPut("beschikbaarheid")]
    public async Task<ActionResult<BeschikbaarheidDto>> BeschikbaarheidBijwerken(
        BeschikbaarheidInvoer invoer, CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }
        if (await _beschikbaarheidValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == medewerkerId, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        if ((medewerker.VasteWerkdagen & invoer.Beschikbaarheidsdagen) != Weekdag.Geen)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Overlap met vaste werkdagen",
                Detail = "Een vaste werkdag kan niet ook als beschikbaarheidsdag worden opgegeven.",
            });
        }

        medewerker.Beschikbaarheidsdagen = invoer.Beschikbaarheidsdagen;
        await _db.SaveChangesAsync(ct);

        return Ok(new BeschikbaarheidDto(
            medewerker.Id, medewerker.VasteWerkdagen, medewerker.Beschikbaarheidsdagen));
    }

    // ---- Verlof self-service --------------------------------------------------

    /// <summary>Het eigen verlofarchief (alle statussen).</summary>
    [HttpGet("verlof")]
    public async Task<ActionResult<IReadOnlyList<VerlofaanvraagDto>>> Verlof(CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        var aanvragen = await _db.Verlofaanvragen.AsNoTracking()
            .Include(a => a.Medewerker)
            .Where(a => a.MedewerkerId == medewerkerId)
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync(ct);

        return Ok(aanvragen.Select(a => VerlofaanvraagMapper.NaarDto(a, Naam(a.Medewerker))).ToList());
    }

    /// <summary>Voor zichzelf verlof aanvragen (komt openstaand binnen bij de planner).</summary>
    [HttpPost("verlof")]
    public async Task<ActionResult<VerlofaanvraagDto>> VerlofAanvragen(ThuisVerlofInvoer invoer, CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        // De medewerker wordt server-side gezet: aanvragen-voor-een-ander is onmogelijk.
        var volledig = new VerlofAanvraagInvoer(
            medewerkerId, invoer.Begindatum, invoer.Einddatum, invoer.AantalUren, invoer.Categorie, invoer.Reden);
        if (await _verlofValidator.ValideerAsync(volledig, this, ct) is { } fout)
        {
            return fout;
        }

        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == medewerkerId, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        var aanvraag = new Verlofaanvraag
        {
            MedewerkerId = medewerkerId,
            Begindatum = invoer.Begindatum,
            Einddatum = invoer.Einddatum,
            AantalUren = invoer.AantalUren,
            Categorie = invoer.Categorie,
            Reden = invoer.Reden,
            Status = VerlofStatus.Openstaand,
        };
        _db.Verlofaanvragen.Add(aanvraag);
        await _db.SaveChangesAsync(ct);

        // Actiecentrum: self-service-aanvraag → planner moet beoordelen (to-do).
        await _meldingen.PubliceerAsync(
            new VerlofAangevraagd(
                aanvraag.Id, Naam(medewerker), aanvraag.Begindatum, aanvraag.Einddatum, aanvraag.AantalUren), ct);

        return CreatedAtAction(nameof(Verlof), null, VerlofaanvraagMapper.NaarDto(aanvraag, Naam(medewerker)));
    }

    /// <summary>Een eigen, nog openstaande verlofaanvraag intrekken.</summary>
    [HttpDelete("verlof/{id:guid}")]
    public async Task<IActionResult> VerlofIntrekken(Guid id, CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        Verlofaanvraag? aanvraag = await _db.Verlofaanvragen.FirstOrDefaultAsync(a => a.Id == id, ct);
        // Niet-eigen aanvraag → NotFound (lekt niet of hij bestaat).
        if (aanvraag is null || aanvraag.MedewerkerId != medewerkerId)
        {
            return NotFound();
        }
        if (aanvraag.Status != VerlofStatus.Openstaand)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Niet intrekbaar",
                Detail = "Alleen een openstaande aanvraag kan worden ingetrokken.",
            });
        }

        _db.Verlofaanvragen.Remove(aanvraag);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Het eigen verlofsaldo per categorie (toegekend/gebruikt/gereserveerd/restant).</summary>
    [HttpGet("saldo")]
    public async Task<ActionResult<IReadOnlyList<VerlofsaldoDto>>> Saldo(CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        var saldi = await _db.Verlofsaldi.AsNoTracking()
            .Where(s => s.MedewerkerId == medewerkerId).ToListAsync(ct);
        var aanvragen = await _db.Verlofaanvragen.AsNoTracking()
            .Where(a => a.MedewerkerId == medewerkerId).ToListAsync(ct);

        var resultaat = new List<VerlofsaldoDto>();
        foreach (VerlofCategorie categorie in Enum.GetValues<VerlofCategorie>())
        {
            Verlofsaldo saldo = saldi.FirstOrDefault(s => s.Categorie == categorie)
                ?? new Verlofsaldo { MedewerkerId = medewerkerId, Categorie = categorie, ToegekendeUren = 0m };
            Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);
            resultaat.Add(VerlofsaldoMapper.NaarDto(medewerkerId, stand));
        }

        return Ok(resultaat);
    }

    // ---- Eigen geregistreerde uren -------------------------------------------

    /// <summary>De eigen urenregistraties in een periode (default: deze week).</summary>
    [HttpGet("uren")]
    public async Task<ActionResult<IReadOnlyList<UrenregistratieDto>>> Uren(
        [FromQuery] DateOnly? van, [FromQuery] DateOnly? tot, CancellationToken ct)
    {
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return Forbid();
        }

        DateOnly vanaf = van ?? WeekplanningBouwer.WeekBeginVan(Vandaag);
        DateOnly totEnMet = tot ?? vanaf.AddDays(6);

        Medewerker? medewerker = await _db.Medewerkers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medewerkerId, ct);

        var registraties = await _db.Urenregistraties.AsNoTracking()
            .Include(u => u.Stamgroep)
            .Where(u => u.MedewerkerId == medewerkerId && u.Datum >= vanaf && u.Datum <= totEnMet)
            .OrderBy(u => u.Datum).ThenBy(u => u.Ingeklokt)
            .ToListAsync(ct);

        IReadOnlyList<UrenregistratieDto> resultaat = registraties
            .Select(u => UrenregistratieMapper.NaarDto(u, Naam(medewerker), u.Stamgroep?.Naam))
            .ToList();
        return Ok(resultaat);
    }

    private static string Naam(Medewerker? m) => m is null ? "" : $"{m.Voornaam} {m.Achternaam}";
}
