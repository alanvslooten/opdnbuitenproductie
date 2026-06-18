using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Meldingen;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Pure vertaling van een <see cref="MeldingGebeurtenis"/> naar een
/// <see cref="Melding"/>. Geen database, geen tijd, geen DI — daardoor volledig
/// unit-testbaar. Hier (en alleen hier) leeft de beslissing of een gebeurtenis een
/// informatieve melding of een af te vinken to-do wordt, plus de weergave-tekst en
/// de deduplicatiesleutel. De dispatcher zet de melding daarna weg (met dedup).
/// </summary>
public static class MeldingFabriek
{
    public static Melding Maak(MeldingGebeurtenis gebeurtenis) => gebeurtenis switch
    {
        NieuweWachtlijstaanmelding e => new Melding
        {
            Soort = MeldingSoort.NieuweWachtlijstaanmelding,
            VereistActie = false,
            Titel = "Nieuwe wachtlijstaanmelding",
            Tekst = $"{e.KindNaam} is op de wachtlijst gezet.",
            BronType = "WachtlijstInschrijving",
            BronId = e.InschrijvingId,
            DeduplicatieSleutel = $"wachtlijst-nieuw:{e.InschrijvingId}",
        },

        VerlofAangevraagd e => new Melding
        {
            Soort = MeldingSoort.Verlofaanvraag,
            VereistActie = true,
            Titel = "Verlofaanvraag beoordelen",
            Tekst = $"{e.MedewerkerNaam} vroeg {e.AantalUren:0.##} uur verlof aan " +
                    $"({e.Begindatum:dd-MM-yyyy} t/m {e.Einddatum:dd-MM-yyyy}).",
            BronType = "Verlofaanvraag",
            BronId = e.AanvraagId,
            DeduplicatieSleutel = $"verlof:{e.AanvraagId}",
        },

        Ziekgemeld e => new Melding
        {
            Soort = MeldingSoort.Ziekmelding,
            VereistActie = true,
            Titel = "Ziekmelding — invaller nodig?",
            Tekst = $"{e.MedewerkerNaam} is ziek gemeld vanaf {e.Begindatum:dd-MM-yyyy}. " +
                    "Controleer of een invaller nodig is.",
            BronType = "Ziekmelding",
            BronId = e.ZiekmeldingId,
            DeduplicatieSleutel = $"ziek:{e.ZiekmeldingId}",
        },

        PlaatsingGeaccepteerd e => new Melding
        {
            Soort = MeldingSoort.VoorstelGeaccepteerd,
            VereistActie = true,
            Titel = "Contract opmaken in Portabase",
            Tekst = e.VolledigGeplaatst
                ? $"{e.KindNaam} is volledig geplaatst (start {e.Startdatum:dd-MM-yyyy}). Maak het contract op in Portabase."
                : $"{e.KindNaam} is deels geplaatst (start {e.Startdatum:dd-MM-yyyy}). Maak het contract op in Portabase.",
            BronType = "WachtlijstInschrijving",
            BronId = e.InschrijvingId,
            // Per (her)plaatsing een eigen to-do: een tweede deelplaatsing met andere
            // startdatum verdient een nieuw contract-item.
            DeduplicatieSleutel = $"plaatsing:{e.InschrijvingId}:{e.Startdatum:yyyy-MM-dd}",
        },

        BkrOverschrijdingGesignaleerd e => new Melding
        {
            Soort = MeldingSoort.BkrWaarschuwing,
            VereistActie = true,
            Titel = "BKR-overschrijding",
            Tekst = $"Groep {e.StamgroepNaam} op {e.Datum:dd-MM-yyyy}: {e.AantalKinderen} kinderen " +
                    $"vragen {e.VereistePmers} pm'ers. Controleer de bezetting.",
            BronType = "Stamgroep",
            BronId = e.StamgroepId,
            DeduplicatieSleutel = $"bkr:{e.StamgroepId}:{e.Datum:yyyy-MM-dd}",
        },

        Observatieherinnering e => new Melding
        {
            Soort = MeldingSoort.Observatieherinnering,
            VereistActie = true,
            Titel = "Observatie inplannen",
            Tekst = $"Voor {e.KindNaam} staat het observatiemoment van {e.MijlpaalMaanden} maanden open.",
            BronType = "Kind",
            BronId = e.KindId,
            DeduplicatieSleutel = $"observatie:{e.KindId}:{e.MijlpaalMaanden}",
        },

        _ => throw new ArgumentOutOfRangeException(
            nameof(gebeurtenis), gebeurtenis, "Onbekend meldingsoort-event."),
    };
}
