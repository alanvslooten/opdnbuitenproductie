using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Instellingen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// De organisatie-instellingen (fase 9c): de instelbare gedragsknoppen die het gedrag
/// van de modules aansturen — meldingen-zichtbaarheid in het actiecentrum, de
/// 'binnenkort'-drempels (observaties + uitstroom), de standaard observatie-mailtekst en
/// de wachtlijst-prioriteitsgewichten. Afgeschermd met
/// <see cref="Capabilities.MagInstellingenBeheren"/> (alleen Beheerder).
///
/// Bestaande data-instellingen (groepen, schoolvakanties, locatie) en de rechten-per-rol
/// matrix krijgen hun eigen endpoints; deze controller beheert het instelbare gedrag dat
/// in de <see cref="OrganisatieInstellingen"/>-entiteit leeft.
/// </summary>
[ApiController]
[Route("api/instellingen")]
[Authorize(Policy = Capabilities.MagInstellingenBeheren)]
public sealed class InstellingenController : ControllerBase
{
    private readonly IInstellingenProvider _provider;
    private readonly KinderKompasDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IValidator<InstellingenInvoer> _validator;
    private readonly IValidator<LocatieInvoer> _locatieValidator;

    public InstellingenController(
        IInstellingenProvider provider,
        KinderKompasDbContext db,
        ITenantProvider tenant,
        IValidator<InstellingenInvoer> validator,
        IValidator<LocatieInvoer> locatieValidator)
    {
        _provider = provider;
        _db = db;
        _tenant = tenant;
        _validator = validator;
        _locatieValidator = locatieValidator;
    }

    /// <summary>De huidige gedragsinstellingen van de organisatie.</summary>
    [HttpGet]
    public async Task<ActionResult<InstellingenDto>> Ophalen(CancellationToken ct)
    {
        OrganisatieInstellingen instellingen = await _provider.HuidigeAsync(ct);
        return Ok(InstellingenMapper.NaarDto(instellingen));
    }

    /// <summary>De gedragsinstellingen bijwerken (volledige vervanging van de knoppen).</summary>
    [HttpPut]
    public async Task<ActionResult<InstellingenDto>> Bijwerken(InstellingenInvoer invoer, CancellationToken ct)
    {
        if (await _validator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        OrganisatieInstellingen instellingen = await _provider.HuidigeAsync(ct);
        InstellingenMapper.PasInvoerToe(instellingen, invoer);
        await _db.SaveChangesAsync(ct);

        return Ok(InstellingenMapper.NaarDto(instellingen));
    }

    // === Locatiegegevens (de Organisatie zelf) ===

    /// <summary>De locatiegegevens (naam + LRK-nummer) van de organisatie.</summary>
    [HttpGet("locatie")]
    public async Task<ActionResult<LocatieDto>> Locatie(CancellationToken ct)
    {
        Organisatie? organisatie = await HuidigeOrganisatieAsync(ct);
        return organisatie is null
            ? NotFound()
            : Ok(new LocatieDto(organisatie.Naam, organisatie.Lrknummer));
    }

    /// <summary>De locatiegegevens bijwerken.</summary>
    [HttpPut("locatie")]
    public async Task<ActionResult<LocatieDto>> LocatieBijwerken(LocatieInvoer invoer, CancellationToken ct)
    {
        if (await _locatieValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        Organisatie? organisatie = await HuidigeOrganisatieAsync(ct);
        if (organisatie is null)
        {
            return NotFound();
        }

        organisatie.Naam = invoer.Naam;
        organisatie.Lrknummer = invoer.Lrknummer;
        await _db.SaveChangesAsync(ct);

        return Ok(new LocatieDto(organisatie.Naam, organisatie.Lrknummer));
    }

    /// <summary>
    /// De organisatie van de huidige tenant. De <see cref="Organisatie"/> is bewust géén
    /// tenant-entiteit (ze valt buiten de globale queryfilter), dus we zoeken 'm expliciet
    /// op via de tenant-sleutel.
    /// </summary>
    private Task<Organisatie?> HuidigeOrganisatieAsync(CancellationToken ct) =>
        _db.Organisaties.FirstOrDefaultAsync(o => o.Id == _tenant.CurrentOrganisatieId, ct);
}
