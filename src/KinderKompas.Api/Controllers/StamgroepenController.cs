using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Stamgroepen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van stamgroepen (CRUD). Afgeschermd met <see cref="Capabilities.MagKinderenBeheren"/>.
/// Alle queries lopen via de tenant-gefilterde DbContext, dus enkel de eigen organisatie.
/// </summary>
[ApiController]
[Route("api/stamgroepen")]
[Authorize(Policy = Capabilities.MagKinderenBeheren)]
public sealed class StamgroepenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<StamgroepInvoer> _validator;

    public StamgroepenController(KinderKompasDbContext db, IValidator<StamgroepInvoer> validator)
    {
        _db = db;
        _validator = validator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StamgroepDto>>> Lijst(CancellationToken ct)
    {
        var groepen = await _db.Stamgroepen
            .AsNoTracking()
            .OrderBy(s => s.Naam)
            .Select(s => new StamgroepDto(s.Id, s.Naam, s.MaxKinderen, s.Kinderen.Count))
            .ToListAsync(ct);

        return Ok(groepen);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StamgroepDto>> Detail(Guid id, CancellationToken ct)
    {
        var groep = await _db.Stamgroepen
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StamgroepDto(s.Id, s.Naam, s.MaxKinderen, s.Kinderen.Count))
            .FirstOrDefaultAsync(ct);

        return groep is null ? NotFound() : Ok(groep);
    }

    [HttpPost]
    public async Task<ActionResult<StamgroepDto>> Aanmaken(StamgroepInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        var groep = new Stamgroep { Naam = invoer.Naam, MaxKinderen = invoer.MaxKinderen };
        _db.Stamgroepen.Add(groep);
        await _db.SaveChangesAsync(ct);

        StamgroepDto dto = StamgroepMapper.NaarDto(groep, 0);
        return CreatedAtAction(nameof(Detail), new { id = groep.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StamgroepDto>> Bewerken(Guid id, StamgroepInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Stamgroep? groep = await _db.Stamgroepen.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (groep is null)
        {
            return NotFound();
        }

        int aantal = await _db.Kinderen.CountAsync(k => k.StamgroepId == id, ct);
        if (invoer.MaxKinderen < aantal)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Maximum te laag",
                Detail = $"De groep heeft al {aantal} kinderen; het maximum kan niet lager dan dat.",
            });
        }

        groep.Naam = invoer.Naam;
        groep.MaxKinderen = invoer.MaxKinderen;
        await _db.SaveChangesAsync(ct);

        return Ok(StamgroepMapper.NaarDto(groep, aantal));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        Stamgroep? groep = await _db.Stamgroepen.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (groep is null)
        {
            return NotFound();
        }

        int aantal = await _db.Kinderen.CountAsync(k => k.StamgroepId == id, ct);
        if (aantal > 0)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Groep niet leeg",
                Detail = $"Er zitten nog {aantal} kinderen in deze groep. Verplaats ze eerst.",
            });
        }

        _db.Stamgroepen.Remove(groep);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
