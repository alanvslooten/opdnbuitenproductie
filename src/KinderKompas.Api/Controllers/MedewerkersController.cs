using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Medewerkers;
using KinderKompas.Application.Portaal;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van medewerkers (CRUD): de stamgegevens, rollen en de twee roosterlagen
/// (vaste werkdagen + beschikbaarheidsdagen) die het auto-rooster van fase 5 voeden.
/// Afgeschermd met <see cref="Capabilities.MagMedewerkersBeheren"/>. Alle queries
/// lopen via de tenant-gefilterde DbContext.
/// </summary>
[ApiController]
[Route("api/medewerkers")]
[Authorize(Policy = Capabilities.MagMedewerkersBeheren)]
public sealed class MedewerkersController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<MedewerkerInvoer> _validator;

    public MedewerkersController(KinderKompasDbContext db, IValidator<MedewerkerInvoer> validator)
    {
        _db = db;
        _validator = validator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedewerkerDto>>> Lijst(CancellationToken ct)
    {
        var medewerkers = await _db.Medewerkers
            .AsNoTracking()
            .Include(m => m.VasteStamgroep)
            .OrderBy(m => m.Achternaam).ThenBy(m => m.Voornaam)
            .ToListAsync(ct);

        return Ok(medewerkers.Select(MedewerkerMapper.NaarDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MedewerkerDto>> Detail(Guid id, CancellationToken ct)
    {
        Medewerker? medewerker = await _db.Medewerkers
            .AsNoTracking()
            .Include(m => m.VasteStamgroep)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return medewerker is null ? NotFound() : Ok(MedewerkerMapper.NaarDto(medewerker));
    }

    [HttpPost]
    public async Task<ActionResult<MedewerkerDto>> Aanmaken(MedewerkerInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        if (await ControleerStamgroep(invoer.VasteStamgroepId, ct) is { } stamgroepFout)
        {
            return stamgroepFout;
        }

        var medewerker = new Medewerker { Voornaam = invoer.Voornaam, Achternaam = invoer.Achternaam };
        MedewerkerMapper.PasInvoerToe(medewerker, invoer);
        _db.Medewerkers.Add(medewerker);
        await _db.SaveChangesAsync(ct);

        // Herladen met groep-navigatie zodat de DTO de groepsnaam bevat.
        await _db.Entry(medewerker).Reference(m => m.VasteStamgroep).LoadAsync(ct);
        MedewerkerDto dto = MedewerkerMapper.NaarDto(medewerker);
        return CreatedAtAction(nameof(Detail), new { id = medewerker.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MedewerkerDto>> Bewerken(Guid id, MedewerkerInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        if (await ControleerStamgroep(invoer.VasteStamgroepId, ct) is { } stamgroepFout)
        {
            return stamgroepFout;
        }

        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        MedewerkerMapper.PasInvoerToe(medewerker, invoer);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(medewerker).Reference(m => m.VasteStamgroep).LoadAsync(ct);
        return Ok(MedewerkerMapper.NaarDto(medewerker));
    }

    /// <summary>
    /// Urenoverzicht van een medewerker over een periode (default: deze maand): gewerkte
    /// (geklokte) uren, verwachte uren op basis van het contract en het saldo
    /// meer-/minderuren, met een uitsplitsing per week. Voedt het medewerkerdossier (F-22).
    /// </summary>
    [HttpGet("{id:guid}/uren")]
    public async Task<ActionResult<UrenoverzichtDto>> Uren(
        Guid id, [FromQuery] DateOnly? van, [FromQuery] DateOnly? tot, CancellationToken ct)
    {
        Medewerker? medewerker = await _db.Medewerkers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        DateOnly vandaag = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly vanaf = van ?? new DateOnly(vandaag.Year, vandaag.Month, 1);
        DateOnly totEnMet = tot ?? vandaag;

        List<Urenregistratie> registraties = await _db.Urenregistraties.AsNoTracking()
            .Where(u => u.MedewerkerId == id && u.Datum >= vanaf && u.Datum <= totEnMet)
            .ToListAsync(ct);

        return Ok(UrenoverzichtBouwer.Bouw(registraties, medewerker.Contracturen, vanaf, totEnMet));
    }

    /// <summary>
    /// Een urenregistratie corrigeren (beheerder): de in-/uitkloktijden achteraf zetten —
    /// ook voor een eerdere dag. Legt vast wie en wanneer corrigeerde (audit).
    /// </summary>
    [HttpPut("uren/{registratieId:guid}/corrigeer")]
    public async Task<ActionResult<UrenregistratieDto>> CorrigeerUren(
        Guid registratieId, UrencorrectieInvoer invoer, CancellationToken ct)
    {
        Urenregistratie? reg = await _db.Urenregistraties
            .Include(u => u.Medewerker).Include(u => u.Stamgroep)
            .FirstOrDefaultAsync(u => u.Id == registratieId, ct);
        if (reg is null)
        {
            return NotFound();
        }

        DateTime ingeklokt = invoer.Ingeklokt.ToUniversalTime();
        DateTime? uitgeklokt = invoer.Uitgeklokt?.ToUniversalTime();
        if (uitgeklokt is { } uit && uit <= ingeklokt)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Ongeldige tijden",
                Detail = "De uitkloktijd moet ná de inkloktijd liggen.",
            });
        }

        reg.Ingeklokt = ingeklokt;
        reg.Uitgeklokt = uitgeklokt;
        reg.GecorrigeerdOp = DateTime.UtcNow;
        reg.GecorrigeerdDoorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _db.SaveChangesAsync(ct);

        string naam = reg.Medewerker is null ? "" : $"{reg.Medewerker.Voornaam} {reg.Medewerker.Achternaam}";
        return Ok(UrenregistratieMapper.NaarDto(reg, naam, reg.Stamgroep?.Naam));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        Medewerker? medewerker = await _db.Medewerkers.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medewerker is null)
        {
            return NotFound();
        }

        // Een medewerker met rooster-/verlofhistorie verwijderen we niet (zou de
        // historie breken); dat is een nette 409 in plaats van een DB-fout.
        bool heeftHistorie =
            await _db.Roosterdiensten.AnyAsync(d => d.MedewerkerId == id, ct) ||
            await _db.Verlofaanvragen.AnyAsync(v => v.MedewerkerId == id, ct) ||
            await _db.Ziekmeldingen.AnyAsync(z => z.MedewerkerId == id, ct);
        if (heeftHistorie)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Medewerker heeft historie",
                Detail = "Deze medewerker heeft rooster-, verlof- of ziektegegevens en kan niet worden verwijderd.",
            });
        }

        _db.Medewerkers.Remove(medewerker);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Controleert dat een opgegeven vaste stamgroep binnen de organisatie bestaat.</summary>
    private async Task<ActionResult?> ControleerStamgroep(Guid? stamgroepId, CancellationToken ct)
    {
        if (stamgroepId is not { } gid)
        {
            return null;
        }

        bool bestaat = await _db.Stamgroepen.AnyAsync(s => s.Id == gid, ct);
        if (!bestaat)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Onbekende stamgroep",
                Detail = "De opgegeven vaste stamgroep bestaat niet.",
            });
        }

        return null;
    }
}
