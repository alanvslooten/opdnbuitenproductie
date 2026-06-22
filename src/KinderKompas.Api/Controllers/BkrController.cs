using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinderKompas.Api.Controllers;

/// <summary>
/// BKR-rekentool (snelrekenaar): een vrije "wat-als" om voor een opgegeven aantal
/// kinderen per leeftijdscategorie het wettelijk vereiste aantal pedagogisch
/// medewerkers te bepalen en dat te toetsen aan het aanwezige personeel. De
/// rekenregels komen volledig uit de geteste <see cref="BkrCalculator"/> in het
/// domein — hier wordt niets gedupliceerd. Afgeschermd met
/// <see cref="Capabilities.MagDashboardZien"/> (back-office), net als in het ontwerp
/// waar de calculator een beheerder-only onderdeel is.
/// </summary>
[ApiController]
[Route("api/bkr")]
[Authorize(Policy = Capabilities.MagDashboardZien)]
public sealed class BkrController : ControllerBase
{
    /// <summary>Berekent de vereiste BKR voor de opgegeven samenstelling.</summary>
    [HttpPost("bereken")]
    public ActionResult<BkrBerekenResultaatDto> Bereken(BkrBerekenInvoer invoer)
    {
        var bandData = new (Leeftijdsgroep Groep, string Label, int Aantal)[]
        {
            (Leeftijdsgroep.NulTotEen, "0-1 jaar", invoer.NulTotEen),
            (Leeftijdsgroep.EenTotTwee, "1-2 jaar", invoer.EenTotTwee),
            (Leeftijdsgroep.TweeTotDrie, "2-3 jaar", invoer.TweeTotDrie),
            (Leeftijdsgroep.DrieTotVier, "3-4 jaar", invoer.DrieTotVier),
        };

        var onderdelen = new List<BkrOnderdeelDto>();
        int totaalKinderen = 0;
        int vereist = 0;
        foreach ((Leeftijdsgroep groep, string label, int aantal) in bandData)
        {
            if (aantal <= 0)
            {
                continue;
            }

            int pmers = BkrCalculator.VereisteVoorEnkeleLeeftijd(groep, aantal);
            onderdelen.Add(new BkrOnderdeelDto(label, aantal, pmers));
            totaalKinderen += aantal;
            vereist += pmers;
        }

        int aanwezig = Math.Max(0, invoer.AanwezigePmers);

        // Status t.o.v. aanwezig personeel. De 3-uursregeling staat een tijdelijke
        // afwijking toe tot maximaal de helft van de vereiste bezetting (en altijd
        // minimaal 1 pm'er). Bron: 3-uursregeling, Bijlage 1.
        string status;
        string melding;
        if (totaalKinderen == 0)
        {
            status = "leeg";
            melding = "Geen kinderen opgegeven.";
        }
        else if (aanwezig >= vereist)
        {
            status = "ok";
            melding = $"BKR in orde — {aanwezig} aanwezig, {vereist} vereist.";
        }
        else if (aanwezig >= (vereist + 1) / 2 && aanwezig >= 1)
        {
            status = "driehuurs";
            melding = "Onder de norm: alleen toegestaan binnen de 3-uursregeling " +
                      "(max. 3 uur per dag, minimaal 1 pm'er aanwezig).";
        }
        else
        {
            status = "overschreden";
            melding = $"BKR overschreden — {aanwezig} aanwezig, {vereist} vereist. Direct actie nodig.";
        }

        return Ok(new BkrBerekenResultaatDto(totaalKinderen, vereist, aanwezig, onderdelen, status, melding));
    }
}

/// <summary>Invoer voor de BKR-snelrekenaar: aantal kinderen per leeftijd + aanwezig personeel.</summary>
public sealed record BkrBerekenInvoer(
    int NulTotEen,
    int EenTotTwee,
    int TweeTotDrie,
    int DrieTotVier,
    int AanwezigePmers);

/// <summary>Eén regel in de uitsplitsing: een leeftijdsband met zijn vereiste pm'ers.</summary>
public sealed record BkrOnderdeelDto(string Label, int AantalKinderen, int VereistePmers);

/// <summary>Uitkomst van de BKR-snelrekenaar.</summary>
public sealed record BkrBerekenResultaatDto(
    int TotaalKinderen,
    int VereisteHoeveelheidPmers,
    int AanwezigePmers,
    IReadOnlyList<BkrOnderdeelDto> Onderdelen,
    string Status,
    string Melding);
