using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Medewerkers;
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

        var medewerker = new Medewerker
        {
            Voornaam = invoer.Voornaam,
            Achternaam = invoer.Achternaam,
            Rol = invoer.Rol,
            VasteWerkdagen = invoer.VasteWerkdagen,
            Beschikbaarheidsdagen = invoer.Beschikbaarheidsdagen,
            Contracturen = invoer.Contracturen,
            VasteStamgroepId = invoer.VasteStamgroepId,
        };
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

        medewerker.Voornaam = invoer.Voornaam;
        medewerker.Achternaam = invoer.Achternaam;
        medewerker.Rol = invoer.Rol;
        medewerker.VasteWerkdagen = invoer.VasteWerkdagen;
        medewerker.Beschikbaarheidsdagen = invoer.Beschikbaarheidsdagen;
        medewerker.Contracturen = invoer.Contracturen;
        medewerker.VasteStamgroepId = invoer.VasteStamgroepId;
        await _db.SaveChangesAsync(ct);

        await _db.Entry(medewerker).Reference(m => m.VasteStamgroep).LoadAsync(ct);
        return Ok(MedewerkerMapper.NaarDto(medewerker));
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
