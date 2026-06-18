using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Meldingen;
using KinderKompas.Application.Verlof;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Meldingen;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Ziekteregistratie: ziekmelding aanmaken, beter melden (einddatum zetten) en
/// corrigeren. Een ziekmelding kan een open einde hebben zolang de medewerker niet
/// hersteld is. Afgeschermd met <see cref="Capabilities.MagRoosterBeheren"/>.
/// </summary>
[ApiController]
[Route("api/ziekmeldingen")]
[Authorize(Policy = Capabilities.MagRoosterBeheren)]
public sealed class ZiekmeldingenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<ZiekmeldingInvoer> _validator;
    private readonly IMeldingDispatcher _meldingen;

    public ZiekmeldingenController(
        KinderKompasDbContext db,
        IValidator<ZiekmeldingInvoer> validator,
        IMeldingDispatcher meldingen)
    {
        _db = db;
        _validator = validator;
        _meldingen = meldingen;
    }

    private static string Naam(Medewerker? m) => m is null ? "" : $"{m.Voornaam} {m.Achternaam}";

    /// <summary>Ziekmeldingen, optioneel gefilterd op medewerker en/of een peildatum waarop ze actief zijn.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ZiekmeldingDto>>> Lijst(
        [FromQuery] Guid? medewerkerId, [FromQuery] DateOnly? actiefOp, CancellationToken ct)
    {
        IQueryable<Ziekmelding> query = _db.Ziekmeldingen.AsNoTracking().Include(z => z.Medewerker);

        if (medewerkerId is { } mid)
        {
            query = query.Where(z => z.MedewerkerId == mid);
        }
        if (actiefOp is { } datum)
        {
            query = query.Where(z => z.Begindatum <= datum && (z.Einddatum == null || datum <= z.Einddatum));
        }

        var meldingen = await query.OrderByDescending(z => z.Begindatum).ToListAsync(ct);
        return Ok(meldingen.Select(z => ZiekmeldingMapper.NaarDto(z, Naam(z.Medewerker))).ToList());
    }

    /// <summary>Een ziekmelding registreren (einddatum optioneel; leeg = nog niet hersteld).</summary>
    [HttpPost]
    public async Task<ActionResult<ZiekmeldingDto>> Registreren(ZiekmeldingInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == invoer.MedewerkerId, ct);
        if (medewerker is null)
        {
            return BadRequest(new ProblemDetails { Title = "Onbekende medewerker", Detail = "De medewerker bestaat niet." });
        }

        var melding = new Ziekmelding
        {
            MedewerkerId = invoer.MedewerkerId,
            Begindatum = invoer.Begindatum,
            Einddatum = invoer.Einddatum,
        };
        _db.Ziekmeldingen.Add(melding);
        await _db.SaveChangesAsync(ct);

        // Actiecentrum: ziekmelding → controleer of een invaller nodig is (to-do).
        await _meldingen.PubliceerAsync(
            new Ziekgemeld(melding.Id, Naam(medewerker), melding.Begindatum), ct);

        return CreatedAtAction(nameof(Lijst), new { medewerkerId = melding.MedewerkerId },
            ZiekmeldingMapper.NaarDto(melding, Naam(medewerker)));
    }

    /// <summary>Beter melden: zet de hersteldatum (laatste ziektedag) op een lopende ziekmelding.</summary>
    [HttpPost("{id:guid}/herstel")]
    public async Task<ActionResult<ZiekmeldingDto>> Herstel(Guid id, ZiekHerstelInvoer invoer, CancellationToken ct)
    {
        Ziekmelding? melding = await _db.Ziekmeldingen.Include(z => z.Medewerker)
            .FirstOrDefaultAsync(z => z.Id == id, ct);
        if (melding is null)
        {
            return NotFound();
        }
        if (invoer.Einddatum < melding.Begindatum)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ongeldige hersteldatum",
                Detail = "De hersteldatum mag niet vóór de begindatum liggen.",
            });
        }

        melding.Einddatum = invoer.Einddatum;
        await _db.SaveChangesAsync(ct);
        return Ok(ZiekmeldingMapper.NaarDto(melding, Naam(melding.Medewerker)));
    }

    /// <summary>Een ziekmelding verwijderen (correctie van een foutieve registratie).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        Ziekmelding? melding = await _db.Ziekmeldingen.FirstOrDefaultAsync(z => z.Id == id, ct);
        if (melding is null)
        {
            return NotFound();
        }

        _db.Ziekmeldingen.Remove(melding);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
