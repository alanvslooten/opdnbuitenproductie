using KinderKompas.Application.Abstractions;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>Eén item in het actiecentrum, klaar voor weergave + deep-link.</summary>
public sealed record MeldingDto(
    Guid Id,
    MeldingSoort Soort,
    MeldingStatus Status,
    bool VereistActie,
    bool IsOpenToDo,
    string Titel,
    string Tekst,
    string? BronType,
    Guid? BronId,
    DateTime AangemaaktOp,
    DateTime? AfgehandeldOp);

/// <summary>Tellers voor het belletje: ongelezen meldingen en openstaande to-do's.</summary>
public sealed record MeldingTellingenDto(int Ongelezen, int OpenToDos);

/// <summary>
/// Het app-brede actiecentrum (fase 9): meldingen en af te vinken to-do's die uit
/// domein-events van de modules ontstaan. Afgeschermd met
/// <see cref="Capabilities.MagDashboardZien"/> — de back-office, niet de portalen.
/// Items worden NIET aangemaakt via deze controller (dat doen de modules via de
/// dispatcher); hier alleen lezen, lezen-markeren en afhandelen.
/// </summary>
[ApiController]
[Route("api/meldingen")]
[Authorize(Policy = Capabilities.MagDashboardZien)]
public sealed class MeldingenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IInstellingenProvider _instellingen;

    public MeldingenController(KinderKompasDbContext db, IInstellingenProvider instellingen)
    {
        _db = db;
        _instellingen = instellingen;
    }

    /// <summary>De meldingsoorten die volgens de instellingen verborgen zijn (als nummers, voor de query).</summary>
    private async Task<List<int>> VerborgenSoortNummersAsync(CancellationToken ct)
    {
        OrganisatieInstellingen instellingen = await _instellingen.HuidigeAsync(ct);
        return instellingen.VerborgenSoorten().Select(s => (int)s).ToList();
    }

    private static MeldingDto NaarDto(Melding m) => new(
        m.Id, m.Soort, m.Status, m.VereistActie, m.IsOpenToDo,
        m.Titel, m.Tekst, m.BronType, m.BronId, m.AangemaaktOp, m.AfgehandeldOp);

    /// <summary>
    /// Het actiecentrum, nieuwste eerst. Standaard verbergt het afgehandelde to-do's;
    /// <paramref name="toonAfgehandeld"/> toont de volledige historie. Met
    /// <paramref name="alleenToDos"/> blijft alleen het to-do-spoor over.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeldingDto>>> Lijst(
        [FromQuery] bool toonAfgehandeld, [FromQuery] bool alleenToDos, CancellationToken ct)
    {
        List<int> verborgen = await VerborgenSoortNummersAsync(ct);

        IQueryable<Melding> query = _db.Meldingen.AsNoTracking()
            .Where(m => !verborgen.Contains((int)m.Soort));

        if (!toonAfgehandeld)
        {
            query = query.Where(m => m.Status != MeldingStatus.Afgehandeld);
        }
        if (alleenToDos)
        {
            query = query.Where(m => m.VereistActie);
        }

        var meldingen = await query
            .OrderByDescending(m => m.AangemaaktOp)
            .ToListAsync(ct);

        return Ok(meldingen.Select(NaarDto).ToList());
    }

    /// <summary>De tellers voor het belletje in de header.</summary>
    [HttpGet("tellingen")]
    public async Task<ActionResult<MeldingTellingenDto>> Tellingen(CancellationToken ct)
    {
        List<int> verborgen = await VerborgenSoortNummersAsync(ct);

        int ongelezen = await _db.Meldingen.CountAsync(
            m => !verborgen.Contains((int)m.Soort) && m.Status == MeldingStatus.Ongelezen, ct);
        int openToDos = await _db.Meldingen.CountAsync(
            m => !verborgen.Contains((int)m.Soort) && m.VereistActie && m.Status != MeldingStatus.Afgehandeld, ct);

        return Ok(new MeldingTellingenDto(ongelezen, openToDos));
    }

    /// <summary>Markeer één melding als gelezen.</summary>
    [HttpPost("{id:guid}/gelezen")]
    public async Task<ActionResult<MeldingDto>> MarkeerGelezen(Guid id, CancellationToken ct)
    {
        Melding? melding = await _db.Meldingen.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (melding is null)
        {
            return NotFound();
        }

        melding.MarkeerGelezen();
        await _db.SaveChangesAsync(ct);
        return Ok(NaarDto(melding));
    }

    /// <summary>Markeer alle ongelezen meldingen als gelezen.</summary>
    [HttpPost("alles-gelezen")]
    public async Task<IActionResult> MarkeerAllesGelezen(CancellationToken ct)
    {
        var ongelezen = await _db.Meldingen
            .Where(m => m.Status == MeldingStatus.Ongelezen)
            .ToListAsync(ct);

        foreach (Melding m in ongelezen)
        {
            m.MarkeerGelezen();
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Vink een to-do af (afhandelen). Werkt alleen op een actie-melding.</summary>
    [HttpPost("{id:guid}/afhandelen")]
    public async Task<ActionResult<MeldingDto>> Afhandelen(Guid id, CancellationToken ct)
    {
        Melding? melding = await _db.Meldingen.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (melding is null)
        {
            return NotFound();
        }
        if (!melding.VereistActie)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Geen to-do",
                Detail = "Deze melding is informatief en kan niet worden afgehandeld; markeer haar als gelezen.",
            });
        }

        melding.HandelAf(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);
        return Ok(NaarDto(melding));
    }
}
