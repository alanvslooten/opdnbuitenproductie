using FluentValidation;
using KinderKompas.Api.Auth;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Stamgroepen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Identity;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van stamgroepen (CRUD). LEZEN mag iedereen die planning/observaties/kinderen
/// gebruikt (<see cref="AutorisatieBeleid.StamgroepenLezen"/>); MUTEREN vereist
/// <see cref="Capabilities.MagKinderenBeheren"/>. Alle queries lopen via de
/// tenant-gefilterde DbContext, dus enkel de eigen organisatie.
/// </summary>
[ApiController]
[Route("api/stamgroepen")]
[Authorize]
public sealed class StamgroepenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<StamgroepInvoer> _validator;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUser _huidigeGebruiker;

    public StamgroepenController(
        KinderKompasDbContext db,
        IValidator<StamgroepInvoer> validator,
        UserManager<ApplicationUser> users,
        ICurrentUser huidigeGebruiker)
    {
        _db = db;
        _validator = validator;
        _users = users;
        _huidigeGebruiker = huidigeGebruiker;
    }

    [HttpGet]
    [Authorize(Policy = AutorisatieBeleid.StamgroepenLezen)]
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
    [Authorize(Policy = AutorisatieBeleid.StamgroepenLezen)]
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
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
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
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
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
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<IActionResult> Verwijderen(Guid id, [FromBody] StamgroepVerwijderInvoer? invoer, CancellationToken ct)
    {
        // Kritieke data: bevestig met het wachtwoord van de ingelogde beheerder.
        if (await VerifieerWachtwoordAsync(invoer?.Wachtwoord) is { } wachtwoordFout)
        {
            return wachtwoordFout;
        }

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

    /// <summary>
    /// Controleert het wachtwoord van de ingelogde gebruiker. Geeft een
    /// <see cref="ActionResult"/> met probleemdetails terug als het ontbreekt of
    /// onjuist is; anders null (in orde).
    /// </summary>
    private async Task<ActionResult?> VerifieerWachtwoordAsync(string? wachtwoord)
    {
        if (string.IsNullOrEmpty(wachtwoord))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bevestiging vereist",
                Detail = "Voer je wachtwoord in om deze actie te bevestigen.",
            });
        }

        ApplicationUser? gebruiker = _huidigeGebruiker.UserId is { } uid
            ? await _users.FindByIdAsync(uid)
            : null;
        if (gebruiker is null || !await _users.CheckPasswordAsync(gebruiker, wachtwoord))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Wachtwoord onjuist",
                Detail = "Het ingevoerde wachtwoord klopt niet.",
            });
        }

        return null;
    }
}

/// <summary>Bevestiging voor het verwijderen van een stamgroep: het beheerderswachtwoord.</summary>
public sealed record StamgroepVerwijderInvoer(string Wachtwoord);
