using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
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
    private const string BijlageMap = "kennisbank";
    private const long MaxBestandsgrootte = 20 * 1024 * 1024; // 20 MB

    private readonly KinderKompasDbContext _db;
    private readonly IValidator<KennisbankInvoer> _validator;
    private readonly ICurrentUser _huidigeGebruiker;
    private readonly IBestandsopslag _opslag;

    public KennisbankController(
        KinderKompasDbContext db, IValidator<KennisbankInvoer> validator,
        ICurrentUser huidigeGebruiker, IBestandsopslag opslag)
    {
        _db = db;
        _validator = validator;
        _huidigeGebruiker = huidigeGebruiker;
        _opslag = opslag;
    }

    // De beheerder ziet en beheert alle documenten; een medewerker ziet alleen
    // documenten voor iedereen of documenten die aan hém zijn toegewezen.
    private bool MagBeheren => _huidigeGebruiker.Heeft(Capabilities.MagInstellingenBeheren);

    private bool MagZien(KennisbankDocument d) => MagBeheren || d.ZichtbaarVoor(_huidigeGebruiker.MedewerkerId);

    /// <summary>De documenten (kort: titel + categorie), gesorteerd op categorie en titel.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KennisbankItemDto>>> Lijst(CancellationToken ct)
    {
        var documenten = await _db.KennisbankDocumenten.AsNoTracking()
            .Include(d => d.Bijlagen)
            .OrderBy(d => d.Categorie).ThenBy(d => d.Titel)
            .ToListAsync(ct);
        return Ok(documenten.Where(MagZien).Select(KennisbankMapper.NaarItem).ToList());
    }

    /// <summary>Eén document met volledige inhoud en bijlagen.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KennisbankDocumentDto>> Detail(Guid id, CancellationToken ct)
    {
        KennisbankDocument? doc = await _db.KennisbankDocumenten.AsNoTracking()
            .Include(d => d.Bijlagen)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null || !MagZien(doc))
        {
            return NotFound();
        }
        return Ok(KennisbankMapper.NaarDto(doc));
    }

    // Normaliseert de toewijzing uit de invoer (lege lijst = voor iedereen).
    private static List<Guid> ToewijzingUit(KennisbankInvoer invoer) =>
        invoer.ToegewezenMedewerkerIds?.Distinct().ToList() ?? new List<Guid>();

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
            ToegewezenMedewerkerIds = ToewijzingUit(invoer),
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
        doc.ToegewezenMedewerkerIds = ToewijzingUit(invoer);
        await _db.SaveChangesAsync(ct);
        return Ok(KennisbankMapper.NaarDto(doc));
    }

    /// <summary>Een document verwijderen (beheerder), inclusief zijn bijlage-bestanden.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    public async Task<IActionResult> Verwijderen(Guid id, CancellationToken ct)
    {
        KennisbankDocument? doc = await _db.KennisbankDocumenten
            .Include(d => d.Bijlagen)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null)
        {
            return NotFound();
        }

        // Eerst de fysieke bestanden opruimen (cascade ruimt alleen de DB-metadata).
        foreach (KennisbankBijlage bijlage in doc.Bijlagen)
        {
            await _opslag.VerwijderAsync(bijlage.BestandsSleutel, ct);
        }

        _db.KennisbankDocumenten.Remove(doc);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Bijlagen ------------------------------------------------------------

    /// <summary>Een bijlage (bestand) bij een document uploaden (beheerder).</summary>
    [HttpPost("{id:guid}/bijlagen")]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    [RequestSizeLimit(MaxBestandsgrootte + 4096)]
    public async Task<ActionResult<KennisbankBijlageDto>> BijlageUploaden(Guid id, IFormFile? bestand, CancellationToken ct)
    {
        KennisbankDocument? doc = await _db.KennisbankDocumenten.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null)
        {
            return NotFound();
        }

        if (bestand is null || bestand.Length == 0)
        {
            return UnprocessableEntity(new ProblemDetails { Title = "Geen bestand", Detail = "Kies een bestand om te uploaden." });
        }
        if (bestand.Length > MaxBestandsgrootte)
        {
            return UnprocessableEntity(new ProblemDetails { Title = "Bestand te groot", Detail = "Een bijlage mag maximaal 20 MB zijn." });
        }

        string sleutel;
        await using (Stream inhoud = bestand.OpenReadStream())
        {
            sleutel = await _opslag.OpslaanAsync(BijlageMap, bestand.FileName, inhoud, ct);
        }

        var bijlage = new KennisbankBijlage
        {
            KennisbankDocumentId = doc.Id,
            BestandsNaam = Path.GetFileName(bestand.FileName),
            BestandsSleutel = sleutel,
            ContentType = string.IsNullOrWhiteSpace(bestand.ContentType) ? "application/octet-stream" : bestand.ContentType,
            BestandsGrootte = bestand.Length,
        };
        _db.KennisbankBijlagen.Add(bijlage);
        await _db.SaveChangesAsync(ct);
        return Ok(KennisbankMapper.NaarBijlage(bijlage));
    }

    /// <summary>Een bijlage downloaden (iedereen die het document mag zien).</summary>
    [HttpGet("bijlagen/{bijlageId:guid}/download")]
    public async Task<IActionResult> BijlageDownloaden(Guid bijlageId, CancellationToken ct)
    {
        KennisbankBijlage? bijlage = await _db.KennisbankBijlagen.AsNoTracking()
            .Include(b => b.Document)
            .FirstOrDefaultAsync(b => b.Id == bijlageId, ct);
        if (bijlage is null || bijlage.Document is null || !MagZien(bijlage.Document))
        {
            return NotFound();
        }

        Stream? inhoud = await _opslag.OpenenAsync(bijlage.BestandsSleutel, ct);
        if (inhoud is null)
        {
            return NotFound();
        }

        // Met bestandsnaam → Content-Disposition attachment: de browser downloadt i.p.v.
        // (mogelijk onveilige) inhoud inline te renderen.
        return File(inhoud, bijlage.ContentType, bijlage.BestandsNaam);
    }

    /// <summary>Een bijlage verwijderen (beheerder).</summary>
    [HttpDelete("bijlagen/{bijlageId:guid}")]
    [Authorize(Policy = Capabilities.MagInstellingenBeheren)]
    public async Task<IActionResult> BijlageVerwijderen(Guid bijlageId, CancellationToken ct)
    {
        KennisbankBijlage? bijlage = await _db.KennisbankBijlagen.FirstOrDefaultAsync(b => b.Id == bijlageId, ct);
        if (bijlage is null)
        {
            return NotFound();
        }

        await _opslag.VerwijderAsync(bijlage.BestandsSleutel, ct);
        _db.KennisbankBijlagen.Remove(bijlage);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
