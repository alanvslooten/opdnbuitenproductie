using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Dashboard;
using KinderKompas.Application.Observaties;
using KinderKompas.Application.Planning;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// Het dashboard (fase 9b): één samengesteld leesmodel met de stand van de dag. ALLE
/// cijfers komen live uit de echte modules — BKR-rekenkern + planning (fase 2/4),
/// het verstuurde rooster (fase 5), de wachtlijst (fase 6), de observaties (fase 7) en
/// het actiecentrum (fase 9a). Geen hardcoded of gemockte waarden. De assemblage zelf
/// zit in de pure <see cref="DashboardBouwer"/>. Afgeschermd met
/// <see cref="Capabilities.MagDashboardZien"/>.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Capabilities.MagDashboardZien)]
public sealed class DashboardController : ControllerBase
{
    private const int MaxRecenteActiviteit = 8;

    private readonly KinderKompasDbContext _db;
    private readonly IInstellingenProvider _instellingen;

    public DashboardController(KinderKompasDbContext db, IInstellingenProvider instellingen)
    {
        _db = db;
        _instellingen = instellingen;
    }

    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Het dashboard voor <paramref name="datum"/> (default: vandaag).</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Ophalen([FromQuery] DateOnly? datum, CancellationToken ct)
    {
        DateOnly peil = datum ?? Vandaag;

        // --- Planning + BKR (per groep per dag, uit de rekenkern) ---
        List<Stamgroep> stamgroepen = await _db.Stamgroepen.AsNoTracking()
            .Include(s => s.Kinderen)
            .OrderBy(s => s.Naam)
            .ToListAsync(ct);
        List<Schoolvakantie> vakanties = await _db.Schoolvakanties.AsNoTracking().ToListAsync(ct);
        WeekplanningDto weekplanning = WeekplanningBouwer.Bouw(peil, stamgroepen, vakanties);

        // --- Verstuurd rooster: aanwezige medewerkers vandaag ---
        DateOnly weekBegin = WeekplanningBouwer.WeekBeginVan(peil);
        Roosterweek? week = await _db.Roosterweken.AsNoTracking()
            .FirstOrDefaultAsync(w => w.WeekBegin == weekBegin, ct);
        bool roosterVerstuurd = week is { Status: RoosterStatus.Verstuurd };

        // Alleen een verstuurd rooster telt als "aanwezig" — concept is nog niet definitief.
        List<Roosterdienst> dienstenVandaag = roosterVerstuurd
            ? await _db.Roosterdiensten.AsNoTracking()
                .Where(d => d.RoosterweekId == week!.Id && d.Datum == peil)
                .ToListAsync(ct)
            : [];

        // De instelbare gedragsknoppen (fase 9c): drempels en meldingen-zichtbaarheid.
        OrganisatieInstellingen inst = await _instellingen.HuidigeAsync(ct);
        List<int> verborgenSoorten = inst.VerborgenSoorten().Select(s => (int)s).ToList();

        // --- Wachtlijst ---
        int aantalWachtend = await _db.Wachtlijstinschrijvingen
            .CountAsync(w => w.Status == WachtlijstStatus.Wachtend, ct);

        // --- Uitstroom: kinderen die binnenkort 4 worden (instelbare drempel) ---
        List<Kind> alleKinderen = stamgroepen.SelectMany(s => s.Kinderen).ToList();
        int binnenkortVier = alleKinderen.Count(k => k.WordtBinnenkortVier(peil, inst.KindBinnenkortVierDrempelDagen));

        // --- Observaties: overschreden/aankomende mijlpalen over alle kinderen (instelbare drempel) ---
        (int observatiesOverschreden, int observatiesBinnenkort) =
            await TelObservatiesAsync(alleKinderen, peil, inst.ObservatieBinnenkortDrempelDagen, ct);

        // --- Actiecentrum (fase 9a) — respecteert de verborgen meldingsoorten (fase 9c) ---
        int openToDos = await _db.Meldingen.CountAsync(
            m => !verborgenSoorten.Contains((int)m.Soort) && m.VereistActie && m.Status != MeldingStatus.Afgehandeld, ct);
        int ongelezen = await _db.Meldingen.CountAsync(
            m => !verborgenSoorten.Contains((int)m.Soort) && m.Status == MeldingStatus.Ongelezen, ct);

        List<ActiviteitDto> recent = await _db.Meldingen.AsNoTracking()
            .Where(m => !verborgenSoorten.Contains((int)m.Soort))
            .OrderByDescending(m => m.AangemaaktOp)
            .Take(MaxRecenteActiviteit)
            .Select(m => new ActiviteitDto(m.Id, m.Soort, m.Titel, m.Tekst, m.AangemaaktOp))
            .ToListAsync(ct);

        var cijfers = new DashboardCijfers(
            aantalWachtend, binnenkortVier, observatiesOverschreden, observatiesBinnenkort,
            openToDos, ongelezen, recent);

        DashboardDto dto = DashboardBouwer.Bouw(peil, weekplanning, roosterVerstuurd, dienstenVandaag, cijfers);
        return Ok(dto);
    }

    /// <summary>
    /// Telt over alle kinderen de overschreden en aankomende observatiemomenten, door
    /// per kind het schema (fase 7) tegen de afgevinkte observaties te berekenen.
    /// </summary>
    private async Task<(int Overschreden, int Binnenkort)> TelObservatiesAsync(
        IReadOnlyCollection<Kind> kinderen, DateOnly peil, int binnenkortDrempelDagen, CancellationToken ct)
    {
        if (kinderen.Count == 0)
        {
            return (0, 0);
        }

        List<Guid> kindIds = kinderen.Select(k => k.Id).ToList();
        List<Observatie> observaties = await _db.Observaties.AsNoTracking()
            .Where(o => kindIds.Contains(o.KindId))
            .ToListAsync(ct);

        Dictionary<Guid, List<Observatie>> perKind = observaties
            .GroupBy(o => o.KindId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int overschreden = 0, binnenkort = 0;
        foreach (Kind kind in kinderen)
        {
            KindObservatieschemaDto schema = ObservatieOverzichtBouwer.Bouw(
                kind, perKind.GetValueOrDefault(kind.Id) ?? [], peil, binnenkortDrempelDagen);
            overschreden += schema.AantalOverschreden;
            binnenkort += schema.AantalBinnenkort;
        }

        return (overschreden, binnenkort);
    }
}
