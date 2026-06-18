using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Meldingen;
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
/// Verlofbeheer: aanvragen indienen, beoordelen (goed-/afkeuren) en het archief met
/// statusfilters, plus het verlofsaldo per medewerker. Afgeschermd met
/// <see cref="Capabilities.MagRoosterBeheren"/> — de planner beheert het verlof.
/// (Self-service aanvragen door medewerkers zelf landt met het thuis-portaal in fase 8.)
/// </summary>
[ApiController]
[Route("api/verlof")]
[Authorize(Policy = Capabilities.MagRoosterBeheren)]
public sealed class VerlofController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<VerlofAanvraagInvoer> _aanvraagValidator;
    private readonly IValidator<VerlofsaldoInvoer> _saldoValidator;
    private readonly IMeldingDispatcher _meldingen;

    public VerlofController(
        KinderKompasDbContext db,
        IValidator<VerlofAanvraagInvoer> aanvraagValidator,
        IValidator<VerlofsaldoInvoer> saldoValidator,
        IMeldingDispatcher meldingen)
    {
        _db = db;
        _aanvraagValidator = aanvraagValidator;
        _saldoValidator = saldoValidator;
        _meldingen = meldingen;
    }

    private static string Naam(Medewerker? m) => m is null ? "" : $"{m.Voornaam} {m.Achternaam}";

    /// <summary>Het verlofarchief, optioneel gefilterd op status en/of medewerker.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VerlofaanvraagDto>>> Lijst(
        [FromQuery] VerlofStatus? status, [FromQuery] Guid? medewerkerId, CancellationToken ct)
    {
        IQueryable<Verlofaanvraag> query = _db.Verlofaanvragen.AsNoTracking().Include(a => a.Medewerker);

        if (status is { } s)
        {
            query = query.Where(a => a.Status == s);
        }
        if (medewerkerId is { } mid)
        {
            query = query.Where(a => a.MedewerkerId == mid);
        }

        var aanvragen = await query
            .OrderByDescending(a => a.AangemaaktOp)
            .ToListAsync(ct);

        return Ok(aanvragen.Select(a => VerlofaanvraagMapper.NaarDto(a, Naam(a.Medewerker))).ToList());
    }

    /// <summary>Een verlofaanvraag indienen (komt openstaand binnen).</summary>
    [HttpPost]
    public async Task<ActionResult<VerlofaanvraagDto>> Aanvragen(VerlofAanvraagInvoer invoer, CancellationToken ct)
    {
        if (await _aanvraagValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == invoer.MedewerkerId, ct);
        if (medewerker is null)
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende medewerker", Detail = "De medewerker bestaat niet." });
        }

        var aanvraag = new Verlofaanvraag
        {
            MedewerkerId = invoer.MedewerkerId,
            Begindatum = invoer.Begindatum,
            Einddatum = invoer.Einddatum,
            AantalUren = invoer.AantalUren,
            Categorie = invoer.Categorie,
            Reden = invoer.Reden,
            Status = VerlofStatus.Openstaand,
        };
        _db.Verlofaanvragen.Add(aanvraag);
        await _db.SaveChangesAsync(ct);

        // Actiecentrum: de planner moet de aanvraag beoordelen (to-do).
        await _meldingen.PubliceerAsync(
            new VerlofAangevraagd(
                aanvraag.Id, Naam(medewerker), aanvraag.Begindatum, aanvraag.Einddatum, aanvraag.AantalUren), ct);

        return CreatedAtAction(nameof(Lijst), new { medewerkerId = aanvraag.MedewerkerId },
            VerlofaanvraagMapper.NaarDto(aanvraag, Naam(medewerker)));
    }

    /// <summary>Een openstaande aanvraag goedkeuren.</summary>
    [HttpPost("{id:guid}/goedkeuren")]
    public Task<ActionResult<VerlofaanvraagDto>> Goedkeuren(Guid id, CancellationToken ct)
        => Beoordeel(id, VerlofStatus.Goedgekeurd, null, ct);

    /// <summary>Een openstaande aanvraag afkeuren (met optionele toelichting).</summary>
    [HttpPost("{id:guid}/afkeuren")]
    public Task<ActionResult<VerlofaanvraagDto>> Afkeuren(Guid id, VerlofBeoordelingInvoer invoer, CancellationToken ct)
        => Beoordeel(id, VerlofStatus.Afgekeurd, invoer.Notitie, ct);

    private async Task<ActionResult<VerlofaanvraagDto>> Beoordeel(
        Guid id, VerlofStatus nieuweStatus, string? notitie, CancellationToken ct)
    {
        Verlofaanvraag? aanvraag = await _db.Verlofaanvragen
            .Include(a => a.Medewerker)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aanvraag is null)
        {
            return NotFound();
        }
        if (aanvraag.Status != VerlofStatus.Openstaand)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Al beoordeeld",
                Detail = $"Deze aanvraag is al {aanvraag.Status} en kan niet opnieuw worden beoordeeld.",
            });
        }

        aanvraag.Status = nieuweStatus;
        aanvraag.BeoordelingsNotitie = notitie;
        aanvraag.BeoordeeldOp = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(VerlofaanvraagMapper.NaarDto(aanvraag, Naam(aanvraag.Medewerker)));
    }

    /// <summary>Een nog openstaande aanvraag intrekken/verwijderen.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Intrekken(Guid id, CancellationToken ct)
    {
        Verlofaanvraag? aanvraag = await _db.Verlofaanvragen.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aanvraag is null)
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

    /// <summary>
    /// Het verlofsaldo van een medewerker: per categorie de toegekende, gebruikte
    /// (goedgekeurde) en gereserveerde (openstaande) uren plus het restant. Categorieën
    /// zonder ingesteld saldo worden als 0 toegekend getoond.
    /// </summary>
    [HttpGet("saldo")]
    public async Task<ActionResult<IReadOnlyList<VerlofsaldoDto>>> Saldo(
        [FromQuery] Guid medewerkerId, CancellationToken ct)
    {
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

    /// <summary>Het toegekende saldo van een medewerker per categorie instellen (upsert).</summary>
    [HttpPut("saldo")]
    public async Task<ActionResult<VerlofsaldoDto>> SaldoInstellen(VerlofsaldoInvoer invoer, CancellationToken ct)
    {
        if (await _saldoValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        bool medewerkerBestaat = await _db.Medewerkers.AnyAsync(m => m.Id == invoer.MedewerkerId, ct);
        if (!medewerkerBestaat)
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende medewerker", Detail = "De medewerker bestaat niet." });
        }

        Verlofsaldo? saldo = await _db.Verlofsaldi
            .FirstOrDefaultAsync(s => s.MedewerkerId == invoer.MedewerkerId && s.Categorie == invoer.Categorie, ct);
        if (saldo is null)
        {
            saldo = new Verlofsaldo { MedewerkerId = invoer.MedewerkerId, Categorie = invoer.Categorie };
            _db.Verlofsaldi.Add(saldo);
        }
        saldo.ToegekendeUren = invoer.ToegekendeUren;
        saldo.Vervaldatum = invoer.Vervaldatum;
        await _db.SaveChangesAsync(ct);

        var aanvragen = await _db.Verlofaanvragen.AsNoTracking()
            .Where(a => a.MedewerkerId == invoer.MedewerkerId).ToListAsync(ct);
        Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);
        return Ok(VerlofsaldoMapper.NaarDto(invoer.MedewerkerId, stand));
    }
}
