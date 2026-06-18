using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Schoolvakanties;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van schoolvakanties (CRUD), per schooljaar. Dit is referentiedata die de
/// planning stuurt (40-wekencontracten worden in vakantieweken niet ingepland), dus
/// afgeschermd met <see cref="Capabilities.MagInstellingenBeheren"/>.
/// </summary>
[ApiController]
[Route("api/schoolvakanties")]
[Authorize(Policy = Capabilities.MagInstellingenBeheren)]
public sealed class SchoolvakantiesController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<SchoolvakantieInvoer> _validator;

    public SchoolvakantiesController(KinderKompasDbContext db, IValidator<SchoolvakantieInvoer> validator)
    {
        _db = db;
        _validator = validator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchoolvakantieDto>>> Lijst(
        [FromQuery] int? schooljaar, CancellationToken ct)
    {
        var query = _db.Schoolvakanties.AsNoTracking();
        if (schooljaar is { } jaar)
        {
            query = query.Where(v => v.Schooljaar == jaar);
        }

        var vakanties = await query
            .OrderBy(v => v.Begindatum)
            .Select(v => new SchoolvakantieDto(
                v.Id, v.Naam, v.Schooljaar, v.Schooljaar + "/" + (v.Schooljaar + 1), v.Begindatum, v.Einddatum))
            .ToListAsync(ct);

        return Ok(vakanties);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchoolvakantieDto>> Detail(Guid id, CancellationToken ct)
    {
        Schoolvakantie? vakantie = await _db.Schoolvakanties.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        return vakantie is null ? NotFound() : Ok(SchoolvakantieMapper.NaarDto(vakantie));
    }

    [HttpPost]
    public async Task<ActionResult<SchoolvakantieDto>> Aanmaken(SchoolvakantieInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        var vakantie = new Schoolvakantie { Naam = invoer.Naam };
        SchoolvakantieMapper.PasInvoerToe(vakantie, invoer);

        _db.Schoolvakanties.Add(vakantie);
        await _db.SaveChangesAsync(ct);

        SchoolvakantieDto dto = SchoolvakantieMapper.NaarDto(vakantie);
        return CreatedAtAction(nameof(Detail), new { id = vakantie.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SchoolvakantieDto>> Bewerken(Guid id, SchoolvakantieInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Schoolvakantie? vakantie = await _db.Schoolvakanties.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vakantie is null)
        {
            return NotFound();
        }

        SchoolvakantieMapper.PasInvoerToe(vakantie, invoer);
        await _db.SaveChangesAsync(ct);

        return Ok(SchoolvakantieMapper.NaarDto(vakantie));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        Schoolvakantie? vakantie = await _db.Schoolvakanties.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vakantie is null)
        {
            return NotFound();
        }

        _db.Schoolvakanties.Remove(vakantie);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
