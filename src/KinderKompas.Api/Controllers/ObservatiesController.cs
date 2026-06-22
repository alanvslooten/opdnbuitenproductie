using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Observaties;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Services;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Observaties: het statusoverzicht per kind (op basis van geboortedatum), het
/// afvinken via PDF-upload, het versturen naar de ouder (stub) en het ongedaan
/// maken. Afgeschermd met <see cref="Capabilities.MagObservatiesVersturen"/>.
///
/// Privacy (fase 3): een leidinggevende (<see cref="Capabilities.MagMedewerkersBeheren"/>)
/// ziet alle kinderen; een gewone medewerker alleen de kinderen waarvan hij mentor is
/// (<c>Kind.MentorId == huidige medewerker</c>). Een gedeeld portaal-account zonder
/// medewerker ziet niets.
/// </summary>
[ApiController]
[Route("api/observaties")]
[Authorize(Policy = Capabilities.MagObservatiesVersturen)]
public sealed class ObservatiesController : ControllerBase
{
    private const long MaxBestandsgrootte = 20 * 1024 * 1024; // 20 MB
    private const string ObservatieMap = "observaties";

    private readonly KinderKompasDbContext _db;
    private readonly ICurrentUser _huidigeGebruiker;
    private readonly IBestandsopslag _opslag;
    private readonly IObservatieMailer _mailer;
    private readonly IInstellingenProvider _instellingen;

    public ObservatiesController(
        KinderKompasDbContext db,
        ICurrentUser huidigeGebruiker,
        IBestandsopslag opslag,
        IObservatieMailer mailer,
        IInstellingenProvider instellingen)
    {
        _db = db;
        _huidigeGebruiker = huidigeGebruiker;
        _opslag = opslag;
        _mailer = mailer;
        _instellingen = instellingen;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Wie alle kinderen mag zien: leidinggevenden (medewerkersbeheer) én het
    /// Groepsportaal op locatie (fase 8). Het portaal is een gedeeld account zonder
    /// eigen mentor, maar bedient álle groepen op de vestiging — overige medewerkers
    /// zien alleen hun mentorkinderen.
    /// </summary>
    private bool MagAllesZien =>
        _huidigeGebruiker.Heeft(Capabilities.MagMedewerkersBeheren) ||
        _huidigeGebruiker.Heeft(Capabilities.MagGroepsportaalGebruiken);

    /// <summary>
    /// De stamgroep waartoe een Groepsportaal-account beperkt is voor BEWERKEN. Een
    /// portaal ziet alle groepen, maar mag alleen de eigen groep afvinken/versturen/
    /// ongedaan maken (feedback Erik V2). Null = geen portaal-beperking (beheerder/mentor).
    /// </summary>
    private Guid? PortaalBewerkGroep =>
        _huidigeGebruiker.Heeft(Capabilities.MagGroepsportaalGebruiken)
            ? _huidigeGebruiker.StamgroepId
            : null;

    /// <summary>De kinderen die de huidige gebruiker mag inzien (mentor-scope).</summary>
    private IQueryable<Kind> ZichtbareKinderen()
    {
        IQueryable<Kind> query = _db.Kinderen.AsNoTracking();
        if (MagAllesZien)
        {
            return query;
        }

        // Geen medewerker (persoonlijk-loos account) → geen eigen mentorkinderen.
        if (_huidigeGebruiker.MedewerkerId is not { } medewerkerId)
        {
            return query.Where(_ => false);
        }

        return query.Where(k => k.MentorId == medewerkerId);
    }

    // ---- Overzicht -----------------------------------------------------------

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KindObservatieschemaDto>>> Overzicht(
        [FromQuery] Guid? stamgroepId, [FromQuery] DateOnly? peildatum, CancellationToken ct)
    {
        DateOnly peil = peildatum ?? Vandaag;

        IQueryable<Kind> query = ZichtbareKinderen();
        if (stamgroepId is { } gid)
        {
            query = query.Where(k => k.StamgroepId == gid);
        }

        List<Kind> kinderen = await query
            .OrderBy(k => k.Achternaam).ThenBy(k => k.Voornaam)
            .ToListAsync(ct);

        List<Guid> kindIds = kinderen.Select(k => k.Id).ToList();
        List<Observatie> observaties = await _db.Observaties.AsNoTracking()
            .Where(o => kindIds.Contains(o.KindId))
            .ToListAsync(ct);

        Dictionary<Guid, List<Observatie>> perKind = observaties
            .GroupBy(o => o.KindId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int drempel = (await _instellingen.HuidigeAsync(ct)).ObservatieBinnenkortDrempelDagen;

        Guid? bewerkGroep = PortaalBewerkGroep;
        IReadOnlyList<KindObservatieschemaDto> resultaat = kinderen
            .Select(k => ObservatieOverzichtBouwer.Bouw(
                    k, perKind.GetValueOrDefault(k.Id) ?? [], peil, drempel)
                with { Bewerkbaar = bewerkGroep is null || k.StamgroepId == bewerkGroep })
            .ToList();

        return Ok(resultaat);
    }

    [HttpGet("kind/{kindId:guid}")]
    public async Task<ActionResult<KindObservatieschemaDto>> VoorKind(
        Guid kindId, [FromQuery] DateOnly? peildatum, CancellationToken ct)
    {
        Kind? kind = await ZichtbareKinderen().FirstOrDefaultAsync(k => k.Id == kindId, ct);
        if (kind is null)
        {
            return NotFound();
        }

        List<Observatie> observaties = await _db.Observaties.AsNoTracking()
            .Where(o => o.KindId == kindId)
            .ToListAsync(ct);

        int drempel = (await _instellingen.HuidigeAsync(ct)).ObservatieBinnenkortDrempelDagen;
        return Ok(ObservatieOverzichtBouwer.Bouw(kind, observaties, peildatum ?? Vandaag, drempel));
    }

    // ---- Afvinken (PDF uploaden) ---------------------------------------------

    [HttpPost("kind/{kindId:guid}/afvinken")]
    [RequestSizeLimit(MaxBestandsgrootte + 4096)]
    public async Task<ActionResult<ObservatieDto>> Afvinken(
        Guid kindId, [FromForm] int mijlpaalMaanden, IFormFile? bestand, CancellationToken ct)
    {
        if (!await MagKindBewerken(kindId, ct))
        {
            return NotFound();
        }

        if (!Observatieschema.Momenten.Any(m => m.MijlpaalMaanden == mijlpaalMaanden))
        {
            return UnprocessableEntity(Probleem("Onbekend observatiemoment",
                $"{mijlpaalMaanden} maanden is geen geldig observatiemoment."));
        }

        if (ValideerBestand(bestand) is { } bestandFout)
        {
            return bestandFout;
        }

        bool bestaatAl = await _db.Observaties
            .AnyAsync(o => o.KindId == kindId && o.MijlpaalMaanden == mijlpaalMaanden, ct);
        if (bestaatAl)
        {
            return Conflict(Probleem("Al afgevinkt",
                "Voor dit moment bestaat al een observatie. Maak die eerst ongedaan om te vervangen."));
        }

        string sleutel;
        await using (Stream inhoud = bestand!.OpenReadStream())
        {
            sleutel = await _opslag.OpslaanAsync(ObservatieMap, bestand.FileName, inhoud, ct);
        }

        var observatie = new Observatie
        {
            KindId = kindId,
            MijlpaalMaanden = mijlpaalMaanden,
            BestandsNaam = Path.GetFileName(bestand.FileName),
            BestandsSleutel = sleutel,
            ContentType = "application/pdf",
            BestandsGrootte = bestand.Length,
        };
        _db.Observaties.Add(observatie);
        await _db.SaveChangesAsync(ct);

        return Ok(ObservatieOverzichtBouwer.NaarDto(observatie));
    }

    // ---- Versturen naar de ouder (stub) --------------------------------------

    [HttpPost("{observatieId:guid}/versturen")]
    public async Task<ActionResult<ObservatieDto>> Versturen(Guid observatieId, CancellationToken ct)
    {
        Observatie? observatie = await _db.Observaties.FirstOrDefaultAsync(o => o.Id == observatieId, ct);
        if (observatie is null || !await MagKindBewerken(observatie.KindId, ct))
        {
            return NotFound();
        }

        Kind? kind = await _db.Kinderen.AsNoTracking().FirstOrDefaultAsync(k => k.Id == observatie.KindId, ct);
        string? email = kind?.Oudercontacten.FirstOrDefault()?.Email;
        if (kind is null || string.IsNullOrWhiteSpace(email))
        {
            return UnprocessableEntity(Probleem("Geen ouder-e-mailadres",
                "Dit kind heeft geen e-mailadres van de ouder; de observatie kan niet verstuurd worden."));
        }

        await using (Stream? inhoud = await _opslag.OpenenAsync(observatie.BestandsSleutel, ct))
        {
            if (inhoud is null)
            {
                return UnprocessableEntity(Probleem("Bestand ontbreekt",
                    "Het PDF-bestand van deze observatie is niet (meer) gevonden in de opslag."));
            }

            string kindNaam = $"{kind.Voornaam} {kind.Achternaam}";
            string? sjabloon = (await _instellingen.HuidigeAsync(ct)).StandaardObservatietekst;
            await _mailer.VerstuurAsync(new ObservatieMail(
                email,
                kindNaam,
                ObservatieMailTekst.Onderwerp(kindNaam),
                ObservatieMailTekst.Bericht(kind.Voornaam, sjabloon),
                observatie.BestandsNaam,
                inhoud), ct);
        }

        observatie.VerzondenOp = DateTime.UtcNow;
        observatie.VerzondenNaarEmail = email;
        await _db.SaveChangesAsync(ct);

        return Ok(ObservatieOverzichtBouwer.NaarDto(observatie));
    }

    // ---- Ongedaan maken (afvinken terugdraaien) ------------------------------

    [HttpDelete("{observatieId:guid}")]
    public async Task<IActionResult> OngedaanMaken(Guid observatieId, CancellationToken ct)
    {
        Observatie? observatie = await _db.Observaties.FirstOrDefaultAsync(o => o.Id == observatieId, ct);
        if (observatie is null || !await MagKindBewerken(observatie.KindId, ct))
        {
            return NotFound();
        }

        // Eerst de DB-rij weg, dan het bestand: een wees-bestand is minder erg dan
        // een rij die naar een verdwenen bestand wijst.
        _db.Observaties.Remove(observatie);
        await _db.SaveChangesAsync(ct);
        await _opslag.VerwijderAsync(observatie.BestandsSleutel, ct);

        return NoContent();
    }

    // ---- PDF downloaden ------------------------------------------------------

    [HttpGet("{observatieId:guid}/bestand")]
    public async Task<IActionResult> Bestand(Guid observatieId, CancellationToken ct)
    {
        Observatie? observatie = await _db.Observaties.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == observatieId, ct);
        if (observatie is null || !await MagKindZien(observatie.KindId, ct))
        {
            return NotFound();
        }

        Stream? inhoud = await _opslag.OpenenAsync(observatie.BestandsSleutel, ct);
        if (inhoud is null)
        {
            return NotFound();
        }

        return File(inhoud, observatie.ContentType, observatie.BestandsNaam);
    }

    // ---- Helpers -------------------------------------------------------------

    private async Task<bool> MagKindZien(Guid kindId, CancellationToken ct) =>
        await ZichtbareKinderen().AnyAsync(k => k.Id == kindId, ct);

    /// <summary>
    /// Of het kind BEWERKT mag worden (afvinken/versturen/ongedaan). Naast de
    /// zichtbaarheid geldt voor een Groepsportaal-account de groepsbeperking: alleen
    /// de eigen groep is bewerkbaar, andere groepen zijn read-only.
    /// </summary>
    private async Task<bool> MagKindBewerken(Guid kindId, CancellationToken ct)
    {
        if (!await MagKindZien(kindId, ct))
        {
            return false;
        }
        if (PortaalBewerkGroep is not { } gid)
        {
            return true;
        }
        return await _db.Kinderen.AsNoTracking().AnyAsync(k => k.Id == kindId && k.StamgroepId == gid, ct);
    }

    private ActionResult? ValideerBestand(IFormFile? bestand)
    {
        if (bestand is null || bestand.Length == 0)
        {
            return UnprocessableEntity(Probleem("Geen bestand", "Upload een PDF-bestand met de observatie."));
        }

        if (bestand.Length > MaxBestandsgrootte)
        {
            return UnprocessableEntity(Probleem("Bestand te groot",
                "Het PDF-bestand mag maximaal 20 MB zijn."));
        }

        bool isPdf =
            string.Equals(Path.GetExtension(bestand.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(bestand.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            return UnprocessableEntity(Probleem("Ongeldig bestandstype",
                "Een observatie moet een PDF-bestand zijn."));
        }

        return null;
    }

    private static ProblemDetails Probleem(string titel, string detail) =>
        new() { Title = titel, Detail = detail };
}
