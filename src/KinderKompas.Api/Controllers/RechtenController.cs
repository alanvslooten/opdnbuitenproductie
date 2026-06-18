using KinderKompas.Application.Instellingen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// De bewerkbare rechten-per-rol matrix (fase 9c): de data-gedreven mapping uit fase 3
/// (<see cref="RolCapability"/>) lezen en per rol aanpassen. Afgeschermd met
/// <see cref="Capabilities.MagInstellingenBeheren"/>. Een wijziging werkt door in het
/// JWT bij de eerstvolgende login/refresh van de betrokken gebruiker. De
/// <see cref="RechtenVangrail"/> voorkomt dat de Beheerder zichzelf buitensluit.
/// </summary>
[ApiController]
[Route("api/instellingen/rechten")]
[Authorize(Policy = Capabilities.MagInstellingenBeheren)]
public sealed class RechtenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;

    public RechtenController(KinderKompasDbContext db)
    {
        _db = db;
    }

    /// <summary>De volledige rechten-matrix: de capability-catalogus + per rol de toegekende rechten.</summary>
    [HttpGet]
    public async Task<ActionResult<RechtenMatrixDto>> Matrix(CancellationToken ct)
    {
        var catalogus = await _db.Capabilities.AsNoTracking()
            .OrderBy(c => c.Sleutel)
            .Select(c => new CapabilityInfoDto(c.Sleutel, c.Omschrijving))
            .ToListAsync(ct);

        List<RolCapability> rolcaps = await _db.RolCapabilities.AsNoTracking()
            .Include(rc => rc.Capability)
            .ToListAsync(ct);

        Dictionary<Rol, List<string>> perRol = rolcaps
            .GroupBy(rc => rc.Rol)
            .ToDictionary(g => g.Key, g => g.Select(rc => rc.Capability!.Sleutel).OrderBy(s => s).ToList());

        var rollen = Enum.GetValues<Rol>()
            .Select(r => new RolRechtenDto(r, perRol.GetValueOrDefault(r) ?? []))
            .ToList();

        return Ok(new RechtenMatrixDto(catalogus, rollen));
    }

    /// <summary>
    /// Vervangt de rechten van één rol volledig door de opgegeven set. Onbekende
    /// sleutels worden geweigerd; voor de Beheerder bewaakt de vangrail dat de
    /// essentiële rechten behouden blijven.
    /// </summary>
    [HttpPut("{rol}")]
    public async Task<ActionResult<RolRechtenDto>> Bijwerken(Rol rol, RolRechtenInvoer invoer, CancellationToken ct)
    {
        if (!Enum.IsDefined(rol))
        {
            return UnprocessableEntity(new ProblemDetails { Title = "Onbekende rol", Detail = "Deze rol bestaat niet." });
        }

        List<string> gevraagd = invoer.Capabilities.Distinct().ToList();

        // Alle sleutels moeten bestaande capabilities zijn.
        Dictionary<string, Guid> bekend = await _db.Capabilities.AsNoTracking()
            .ToDictionaryAsync(c => c.Sleutel, c => c.Id, ct);

        List<string> onbekend = gevraagd.Where(s => !bekend.ContainsKey(s)).ToList();
        if (onbekend.Count > 0)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Onbekende capability",
                Detail = $"Onbekende rechten: {string.Join(", ", onbekend)}.",
            });
        }

        // Anti-uitsluiting: de Beheerder moet z'n essentiële rechten houden.
        IReadOnlyList<string> ontbreekt = RechtenVangrail.OntbrekendeBeschermdeRechten(rol, gevraagd);
        if (ontbreekt.Count > 0)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Beheerder mag dit recht niet verliezen",
                Detail = $"De Beheerder moet deze rechten houden: {string.Join(", ", ontbreekt)}.",
            });
        }

        // Verschil bepalen tegen de bestaande rijen en alleen dat muteren.
        List<RolCapability> bestaand = await _db.RolCapabilities.Where(rc => rc.Rol == rol).ToListAsync(ct);
        HashSet<Guid> doelIds = gevraagd.Select(s => bekend[s]).ToHashSet();
        HashSet<Guid> bestaandeIds = bestaand.Select(rc => rc.CapabilityId).ToHashSet();

        foreach (RolCapability overbodig in bestaand.Where(rc => !doelIds.Contains(rc.CapabilityId)))
        {
            _db.RolCapabilities.Remove(overbodig);
        }

        foreach (Guid nieuwId in doelIds.Where(id => !bestaandeIds.Contains(id)))
        {
            // OrganisatieId wordt centraal door de DbContext gezet.
            _db.RolCapabilities.Add(new RolCapability { Rol = rol, CapabilityId = nieuwId });
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new RolRechtenDto(rol, gevraagd.OrderBy(s => s).ToList()));
    }
}
