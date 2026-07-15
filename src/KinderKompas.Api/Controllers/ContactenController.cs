using KinderKompas.Api.Auth;
using KinderKompas.Application.Contacten;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// De Contacten-module: het CRM-knooppunt voor ouders/verzorgers/voogden. Per contact
/// de contactgegevens, intern/extern, aantekeningen en de historie: rondleidingen,
/// wachtlijst-inschrijvingen (met hun voorstellen) en geplaatste kinderen. Afgeschermd
/// met <see cref="Capabilities.MagWachtlijstBeheren"/> (Beheerder/Hulpbeheerder).
/// </summary>
[ApiController]
[Route("api/contacten")]
[Authorize(Policy = Capabilities.MagWachtlijstBeheren)]
public sealed class ContactenController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly WachtwoordChecker _wachtwoord;

    public ContactenController(KinderKompasDbContext db, WachtwoordChecker wachtwoord)
    {
        _db = db;
        _wachtwoord = wachtwoord;
    }

    private static string Naam(string voornaam, string achternaam) => $"{voornaam} {achternaam}".Trim();

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> Lijst(CancellationToken ct)
    {
        List<ContactDto> contacten = await _db.Contacten.AsNoTracking()
            .OrderBy(c => c.Achternaam).ThenBy(c => c.Voornaam)
            .Select(c => new ContactDto(
                c.Id, c.Voornaam, c.Achternaam, c.Voornaam + " " + c.Achternaam,
                c.Telefoon, c.Email, c.IsIntern, c.Aantekeningen,
                c.Rondleidingen.Count, c.Inschrijvingen.Count, c.Kinderen.Count))
            .ToListAsync(ct);
        return Ok(contacten);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContactDetailDto>> Detail(Guid id, CancellationToken ct)
    {
        Contact? c = await _db.Contacten.AsNoTracking()
            .Include(x => x.Rondleidingen)
            .Include(x => x.Inschrijvingen).ThenInclude(i => i.Voorstellen)
            .Include(x => x.Kinderen).ThenInclude(k => k.Stamgroep)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
        {
            return NotFound();
        }

        var logboek = await _db.ContactLogregels.AsNoTracking()
            .Where(l => l.ContactId == id)
            .OrderByDescending(l => l.AangemaaktOp)
            .Select(l => new ContactLogregelDto(l.AangemaaktOp, l.Omschrijving))
            .ToListAsync(ct);

        var detail = new ContactDetailDto(
            c.Id, c.Voornaam, c.Achternaam, c.Telefoon, c.Email, c.IsIntern, c.Aantekeningen,
            c.Rondleidingen
                .OrderByDescending(r => r.Datum)
                .Select(r => new RondleidingDto(r.Id, r.Datum, (int)r.Status, r.Notitie))
                .ToList(),
            c.Inschrijvingen
                .OrderByDescending(i => i.InschrijfdatumWachtlijst)
                .Select(i => new ContactInschrijvingDto(
                    i.Id, Naam(i.Voornaam, i.Achternaam), i.GewensteStartdatum, (int)i.Status, i.Voorstellen.Count))
                .ToList(),
            c.Kinderen
                .OrderBy(k => k.Achternaam)
                .Select(k => new ContactKindDto(k.Id, Naam(k.Voornaam, k.Achternaam), k.Stamgroep!.Naam))
                .ToList(),
            logboek);

        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Aanmaken(ContactInvoer invoer, CancellationToken ct)
    {
        if (Valideer(invoer) is { } fout)
        {
            return fout;
        }

        var contact = new Contact
        {
            Voornaam = invoer.Voornaam.Trim(),
            Achternaam = invoer.Achternaam.Trim(),
            Telefoon = Leeg(invoer.Telefoon),
            Email = Leeg(invoer.Email),
            IsIntern = invoer.IsIntern,
            Aantekeningen = Leeg(invoer.Aantekeningen),
        };
        _db.Contacten.Add(contact);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Detail), new { id = contact.Id }, NaarDto(contact));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContactDto>> Bewerken(Guid id, ContactInvoer invoer, CancellationToken ct)
    {
        if (Valideer(invoer) is { } fout)
        {
            return fout;
        }

        Contact? contact = await _db.Contacten.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contact is null)
        {
            return NotFound();
        }

        contact.Voornaam = invoer.Voornaam.Trim();
        contact.Achternaam = invoer.Achternaam.Trim();
        contact.Telefoon = Leeg(invoer.Telefoon);
        contact.Email = Leeg(invoer.Email);
        contact.IsIntern = invoer.IsIntern;
        contact.Aantekeningen = Leeg(invoer.Aantekeningen);
        await _db.SaveChangesAsync(ct);

        return Ok(NaarDto(contact));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Verwijderen(Guid id, [FromBody] BevestigInvoer? invoer, CancellationToken ct)
    {
        // Kritieke data: bevestig met het wachtwoord van de ingelogde beheerder.
        if (!await _wachtwoord.KloptHuidigeGebruikerAsync(invoer?.Wachtwoord))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Bevestiging vereist",
                Detail = "Voer je wachtwoord in om dit contact te verwijderen.",
            });
        }

        Contact? contact = await _db.Contacten.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contact is null)
        {
            return NotFound();
        }

        // Gekoppelde kinderen/inschrijvingen blijven bestaan (ContactId → null); de
        // rondleidingen van dit contact gaan mee (cascade).
        _db.Contacten.Remove(contact);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/rondleidingen")]
    public async Task<ActionResult<RondleidingDto>> RondleidingToevoegen(
        Guid id, RondleidingInvoer invoer, CancellationToken ct)
    {
        if (!await _db.Contacten.AnyAsync(c => c.Id == id, ct))
        {
            return NotFound();
        }
        if (!Enum.IsDefined(typeof(RondleidingStatus), invoer.Status))
        {
            return UnprocessableEntity(new ProblemDetails { Title = "Ongeldige status" });
        }

        var rondleiding = new Rondleiding
        {
            ContactId = id,
            Datum = invoer.Datum,
            Status = (RondleidingStatus)invoer.Status,
            Notitie = Leeg(invoer.Notitie),
        };
        _db.Rondleidingen.Add(rondleiding);
        await _db.SaveChangesAsync(ct);

        return Ok(new RondleidingDto(rondleiding.Id, rondleiding.Datum, (int)rondleiding.Status, rondleiding.Notitie));
    }

    [HttpDelete("rondleidingen/{rondleidingId:guid}")]
    public async Task<IActionResult> RondleidingVerwijderen(Guid rondleidingId, CancellationToken ct)
    {
        Rondleiding? r = await _db.Rondleidingen.FirstOrDefaultAsync(x => x.Id == rondleidingId, ct);
        if (r is null)
        {
            return NotFound();
        }
        _db.Rondleidingen.Remove(r);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Leeg(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private ActionResult? Valideer(ContactInvoer invoer)
    {
        if (string.IsNullOrWhiteSpace(invoer.Voornaam) || string.IsNullOrWhiteSpace(invoer.Achternaam))
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Naam verplicht",
                Detail = "Voor- en achternaam van het contact zijn verplicht.",
            });
        }
        return null;
    }

    private static ContactDto NaarDto(Contact c) => new(
        c.Id, c.Voornaam, c.Achternaam, c.VolledigeNaam, c.Telefoon, c.Email,
        c.IsIntern, c.Aantekeningen,
        c.Rondleidingen.Count, c.Inschrijvingen.Count, c.Kinderen.Count);
}
