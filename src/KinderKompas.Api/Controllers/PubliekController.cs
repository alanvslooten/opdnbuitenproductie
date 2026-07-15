using FluentValidation;
using KinderKompas.Api.Validatie;
using KinderKompas.Application.Publiek;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// PUBLIEKE (anonieme) formulieren: ouders melden zelf een kind aan op de wachtlijst of
/// vragen een rondleiding aan (vervangt de Portabase-instroom). Bewust hard begrensd met
/// rate limiting tegen misbruik, en met een beperkte veldenset. De aanvragen komen binnen
/// op de organisatie en worden dáár verder afgehandeld (prioriteit, groep, contact).
///
/// Single-tenant: de aanvragen landen op de vaste seed-organisatie. Bij een echte
/// multi-tenant publieke site zou de organisatie via een publieke sleutel in de URL komen.
/// </summary>
[ApiController]
[Route("api/publiek")]
[AllowAnonymous]
[EnableRateLimiting("publiek")]
public sealed class PubliekController : ControllerBase
{
    private readonly KinderKompasDbContext _db;
    private readonly IValidator<PubliekeAanmeldingInvoer> _aanmeldValidator;
    private readonly IValidator<PubliekeRondleidingInvoer> _rondleidingValidator;

    public PubliekController(
        KinderKompasDbContext db,
        IValidator<PubliekeAanmeldingInvoer> aanmeldValidator,
        IValidator<PubliekeRondleidingInvoer> rondleidingValidator)
    {
        _db = db;
        _aanmeldValidator = aanmeldValidator;
        _rondleidingValidator = rondleidingValidator;
    }

    private static Guid DoelOrganisatie => SeedConstanten.OrganisatieId;
    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Publieke wachtlijst-aanmelding door een ouder.</summary>
    [HttpPost("aanmelden")]
    public async Task<IActionResult> Aanmelden(PubliekeAanmeldingInvoer invoer, CancellationToken ct)
    {
        if (await _aanmeldValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        var inschrijving = new WachtlijstInschrijving
        {
            // Anonieme context: expliciet de doel-organisatie zetten (de tenant-provider
            // heeft geen claim). Zo slaat ZetTenantEnAudit het provider-lookup over.
            OrganisatieId = DoelOrganisatie,
            Voornaam = invoer.Voornaam.Trim(),
            Achternaam = invoer.Achternaam.Trim(),
            Geboortedatum = invoer.Geboortedatum,
            InschrijfdatumWachtlijst = Vandaag,
            GewensteStartdatum = invoer.GewensteStartdatum,
            GewensteOpvangdagen = invoer.GewensteOpvangdagen,
            Contracttype = invoer.Contracttype,
            Status = WachtlijstStatus.Wachtend,
            Oudercontact = new Oudercontact(invoer.OuderNaam.Trim(), invoer.OuderTelefoon.Trim(), invoer.OuderEmail.Trim()),
            Notitie = string.IsNullOrWhiteSpace(invoer.Opmerking)
                ? "Via publiek aanmeldformulier."
                : $"Via publiek aanmeldformulier. {invoer.Opmerking.Trim()}",
        };
        _db.Wachtlijstinschrijvingen.Add(inschrijving);
        await _db.SaveChangesAsync(ct);

        // Bewust geen gegevens teruggeven aan de anonieme beller.
        return Ok(new { ontvangen = true });
    }

    /// <summary>Publieke rondleiding-aanvraag door een ouder.</summary>
    [HttpPost("rondleiding")]
    public async Task<IActionResult> Rondleiding(PubliekeRondleidingInvoer invoer, CancellationToken ct)
    {
        if (await _rondleidingValidator.ValideerAsync(invoer, this, ct) is { } fout)
        {
            return fout;
        }

        var contact = new Contact
        {
            OrganisatieId = DoelOrganisatie,
            Voornaam = invoer.OuderVoornaam.Trim(),
            Achternaam = invoer.OuderAchternaam.Trim(),
            Telefoon = invoer.Telefoon.Trim(),
            Email = invoer.Email.Trim(),
            IsIntern = false,
            Aantekeningen = "Aangemaakt via publiek rondleidingformulier.",
        };
        var rondleiding = new Rondleiding
        {
            OrganisatieId = DoelOrganisatie,
            Contact = contact,
            Datum = invoer.VoorkeurDatum,
            Status = RondleidingStatus.Gepland,
            Notitie = string.IsNullOrWhiteSpace(invoer.Opmerking) ? null : invoer.Opmerking.Trim(),
        };
        contact.Rondleidingen.Add(rondleiding);
        _db.Contacten.Add(contact);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ontvangen = true });
    }
}
