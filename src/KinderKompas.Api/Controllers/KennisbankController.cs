using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Kennisbank;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Interne kennisbank (v3): read-only naslag voor medewerkers — protocollen, het
/// pedagogisch beleidsplan, kledingvoorschriften e.d. Lezen mag iedereen met een
/// (thuis)portaal-account (<see cref="Capabilities.MagThuisportaalGebruiken"/>);
/// beheren is voorbehouden aan de beheerder (<see cref="Capabilities.MagInstellingenBeheren"/>).
/// </summary>
[ApiController]
[Route("api/kennisbank")]
[Authorize(Policy = Capabilities.MagThuisportaalGebruiken)]
public sealed class KennisbankController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<KennisbankInvoer> _validator;

    public KennisbankController(KinderKompasDbContext db, IValidator<KennisbankInvoer> validator)
    {
        _db = db;
        _validator = validator;
    }

    /// <summary>De documenten (kort: titel + categorie), gesorteerd op categorie en titel.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KennisbankItemDto>>> Lijst(CancellationToken ct)
    {
        var documenten = await _db.KennisbankDocumenten.AsNoTracking()
            .OrderBy(d => d.Categorie).ThenBy(d => d.Titel)
            .ToListAsync(ct);
        return Ok(documenten.Select(KennisbankMapper.NaarItem).ToList());
    }

    /// <summary>Eén document met volledige inhoud.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KennisbankDocumentDto>> Detail(Guid id, CancellationToken ct)
    {
        KennisbankDocument? doc = await _db.KennisbankDocumenten.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return doc is null ? NotFound() : Ok(KennisbankMapper.NaarDto(doc));
    }

    /// <summary>Een document toevoegen (beheerder).</summary>
    [HttpPost]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    public async Task<ActionResult<KennisbankDocumentDto>> Toevoegen(KennisbankInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        var doc = new KennisbankDocument
        {
            Titel = invoer.Titel.Trim(),
            Categorie = string.IsNullOrWhiteSpace(invoer.Categorie) ? null : invoer.Categorie.Trim(),
            Inhoud = invoer.Inhoud,
        };
        _db.KennisbankDocumenten.Add(doc);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = doc.Id }, KennisbankMapper.NaarDto(doc));
    }

    /// <summary>Een document bijwerken (beheerder).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    public async Task<ActionResult<KennisbankDocumentDto>> Bijwerken(Guid id, KennisbankInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        KennisbankDocument? doc = await _db.KennisbankDocumenten.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null)
        {
            return NotFound();
        }

        doc.Titel = invoer.Titel.Trim();
        doc.Categorie = string.IsNullOrWhiteSpace(invoer.Categorie) ? null : invoer.Categorie.Trim();
        doc.Inhoud = invoer.Inhoud;
        await _db.SaveChangesAsync(ct);
        return Ok(KennisbankMapper.NaarDto(doc));
    }

    /// <summary>Een document verwijderen (beheerder).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        KennisbankDocument? doc = await _db.KennisbankDocumenten.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null)
        {
            return NotFound();
        }

        _db.KennisbankDocumenten.Remove(doc);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
