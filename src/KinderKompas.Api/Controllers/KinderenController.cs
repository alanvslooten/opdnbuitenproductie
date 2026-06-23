using FluentValidation;
using KinderKompas.Api.Auth;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Beheer van kinderen (CRUD), input voor de planning. Afgeschermd met
/// <see cref="Capabilities.MagKinderenBeheren"/>. De privacy-scheiding van
/// oudergegevens loopt via <see cref="KindMapper"/> + <see cref="ICurrentUser"/>;
/// de "13e plaatsing" (groepsmaximum) wordt hier tegen de database gecontroleerd.
/// </summary>
[ApiController]
[Route("api/kinderen")]
[Authorize]
public sealed class KinderenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly ICurrentUser _huidigeGebruiker;
    private readonly IValidator<KindInvoer> _validator;
    private readonly WachtwoordChecker _wachtwoord;

    public KinderenController(
        KinderKompasDbContext db, ICurrentUser huidigeGebruiker, IValidator<KindInvoer> validator,
        WachtwoordChecker wachtwoord)
    {
        _db = db;
        _huidigeGebruiker = huidigeGebruiker;
        _validator = validator;
        _wachtwoord = wachtwoord;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Voor een Groepsportaal-account (alleen-lezen) zijn kinderen beperkt tot de eigen
    /// stamgroep; back-office (KinderenBeheren) ziet alle groepen.
    /// </summary>
    private Guid? PortaalGroep =>
        _huidigeGebruiker.Heeft(Capabilities.MagGroepsportaalGebruiken) && !_huidigeGebruiker.Heeft(Capabilities.MagKinderenBeheren)
            ? _huidigeGebruiker.StamgroepId
            : null;

    [HttpGet]
    [Authorize(Policy = AutorisatieBeleid.KinderenLezen)]
    public async Task<ActionResult<IReadOnlyList<KindDto>>> Lijst(
        [FromQuery] Guid? stamgroepId, CancellationToken ct)
    {
        Guid? effectieveGroep = PortaalGroep ?? stamgroepId;
        var query = _db.Kinderen.AsNoTracking();
        if (effectieveGroep is { } gid)
        {
            query = query.Where(k => k.StamgroepId == gid);
        }

        var kinderen = await query
            .OrderBy(k => k.Achternaam).ThenBy(k => k.Voornaam)
            .ToListAsync(ct);

        IReadOnlyList<KindDto> resultaat =
            kinderen.Select(k => KindMapper.NaarDto(k, _huidigeGebruiker, Vandaag)).ToList();

        return Ok(resultaat);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AutorisatieBeleid.KinderenLezen)]
    public async Task<ActionResult<KindDto>> Detail(Guid id, CancellationToken ct)
    {
        Kind? kind = await _db.Kinderen.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (kind is null)
        {
            return NotFound();
        }
        // Een gescoped portaal mag alleen de eigen-groep-kinderen inzien.
        if (PortaalGroep is { } gid && kind.StamgroepId != gid)
        {
            return NotFound();
        }
        return Ok(KindMapper.NaarDto(kind, _huidigeGebruiker, Vandaag));
    }

    [HttpPost]
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<ActionResult<KindDto>> Aanmaken(KindInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        if (await ControleerStamgroepEnPlaats(invoer.StamgroepId, bestaandKindId: null, ct) is { } plaatsFout)
        {
            return plaatsFout;
        }

        var kind = new Kind
        {
            Voornaam = invoer.Voornaam,
            Achternaam = invoer.Achternaam,
        };
        KindMapper.PasInvoerToe(kind, invoer);

        _db.Kinderen.Add(kind);
        await _db.SaveChangesAsync(ct);

        KindDto dto = KindMapper.NaarDto(kind, _huidigeGebruiker, Vandaag);
        return CreatedAtAction(nameof(Detail), new { id = kind.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<ActionResult<KindDto>> Bewerken(Guid id, KindInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Kind? kind = await _db.Kinderen.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (kind is null)
        {
            return NotFound();
        }

        if (await ControleerStamgroepEnPlaats(invoer.StamgroepId, bestaandKindId: id, ct) is { } plaatsFout)
        {
            return plaatsFout;
        }

        KindMapper.PasInvoerToe(kind, invoer);
        await _db.SaveChangesAsync(ct);

        return Ok(KindMapper.NaarDto(kind, _huidigeGebruiker, Vandaag));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Capabilities.MagKinderenBeheren)]
    public async Task<IActionResult> Verwijderen(Guid id, [FromBody] BevestigInvoer? invoer, CancellationToken ct)
    {
        // Kritieke data: bevestig met het wachtwoord van de ingelogde beheerder.
        if (!await _wachtwoord.KloptHuidigeGebruikerAsync(invoer?.Wachtwoord))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Bevestiging vereist",
                Detail = "Voer je wachtwoord in om dit kind te verwijderen.",
            });
        }

        Kind? kind = await _db.Kinderen.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (kind is null)
        {
            return NotFound();
        }

        _db.Kinderen.Remove(kind);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Controleert dat de stamgroep bestaat en dat plaatsing het groepsmaximum niet
    /// overschrijdt (de "13e plaatsing"). Bij een update telt het kind zelf niet mee.
    /// Geeft <c>null</c> als alles in orde is, anders een passend foutresultaat.
    /// </summary>
    private async Task<ActionResult?> ControleerStamgroepEnPlaats(
        Guid stamgroepId, Guid? bestaandKindId, CancellationToken ct)
    {
        Stamgroep? groep = await _db.Stamgroepen.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stamgroepId, ct);
        if (groep is null)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Onbekende stamgroep",
                Detail = "De opgegeven stamgroep bestaat niet binnen deze organisatie.",
            });
        }

        // Bij een update telt het kind zelf niet mee in de bezetting; bij aanmaken (null)
        // tellen alle huidige kinderen mee. NB: vergelijken met een null-Guid in SQL
        // levert geen rijen, dus de twee gevallen bewust splitsen.
        int huidigAantal = bestaandKindId is { } bestaand
            ? await _db.Kinderen.CountAsync(k => k.StamgroepId == stamgroepId && k.Id != bestaand, ct)
            : await _db.Kinderen.CountAsync(k => k.StamgroepId == stamgroepId, ct);

        if (!groep.HeeftPlaatsVoorExtraKind(huidigAantal))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Groep vol",
                Detail = $"Stamgroep '{groep.Naam}' zit vol ({huidigAantal}/{groep.MaxKinderen}). " +
                         "Plaatsing zou het groepsmaximum overschrijden.",
            });
        }

        return null;
    }
}
