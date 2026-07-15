using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Exceptions;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>
/// Bouwt de controle-analyse voor de voorstel-pop-up uit reeds geladen domeingegevens.
/// Pure functie zonder database- of UI-afhankelijkheid: de controller laadt de
/// inschrijving, de doel-stamgroep (met haar geplaatste kinderen) en de
/// schoolvakanties en geeft die hier door.
///
/// <para>Alle BKR-uitkomsten komen rechtstreeks uit de
/// <see cref="BkrCalculator"/> (huidig én via <see cref="GroepSamenstelling.MetExtra"/>
/// mét de kandidaat erbij); hier zit géén eigen rekenregel. Zo komt de BKR-impact
/// in de pop-up gegarandeerd exact overeen met de wettelijke rekenkern.</para>
/// </summary>
public static class VoorstelAnalyseBouwer
{
    private static readonly Weekdag[] Opvangdagen =
    {
        Weekdag.Maandag, Weekdag.Dinsdag, Weekdag.Woensdag, Weekdag.Donderdag, Weekdag.Vrijdag
    };

    /// <param name="inschrijving">De wachtlijst-inschrijving (de kandidaat + aanvraag).</param>
    /// <param name="doelStamgroep">De stamgroep waarin geplaatst zou worden, mét <c>Kinderen</c> geladen.</param>
    /// <param name="vakanties">De schoolvakanties (voor 40-wekencontracten in de bezetting).</param>
    /// <param name="peilStartdatum">
    /// Optionele afwijkende startdatum om mee te rekenen (default: de gewenste
    /// startdatum van de inschrijving). Laat de planner de impact van een andere
    /// ingangsdatum bekijken.
    /// </param>
    /// <param name="openVoorstelKinderen">
    /// Voorlopige bezetting uit nog OPENSTAANDE (verstuurde, niet-geaccepteerde)
    /// voorstellen voor deze groep, als transiënte kinderen. Ze tellen mee in de
    /// bezetting en BKR per dag, zodat twee tegelijk lopende voorstellen niet samen
    /// ongemerkt de BKR overschrijden. <c>null</c> = niets extra meetellen.
    /// </param>
    public static VoorstelAnalyseDto Bouw(
        WachtlijstInschrijving inschrijving,
        Stamgroep doelStamgroep,
        IEnumerable<Schoolvakantie> vakanties,
        DateOnly? peilStartdatum = null,
        IEnumerable<Kind>? openVoorstelKinderen = null)
    {
        ArgumentNullException.ThrowIfNull(inschrijving);
        ArgumentNullException.ThrowIfNull(doelStamgroep);
        ArgumentNullException.ThrowIfNull(vakanties);

        DateOnly start = peilStartdatum ?? inschrijving.GewensteStartdatum;
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();
        IReadOnlyList<Kind> geplaatst =
            doelStamgroep.Kinderen as IReadOnlyList<Kind> ?? doelStamgroep.Kinderen.ToList();
        IReadOnlyList<Kind> voorlopig =
            openVoorstelKinderen as IReadOnlyList<Kind> ?? openVoorstelKinderen?.ToList() ?? [];

        // De bezetting die per dag telt = de al geplaatste kinderen + de voorlopige
        // kinderen uit openstaande voorstellen (zo blijft de BKR-projectie eerlijk).
        IReadOnlyList<Kind> bezetting = voorlopig.Count == 0 ? geplaatst : geplaatst.Concat(voorlopig).ToList();

        bool kandidaatBuitenLeeftijd =
            !Leeftijdscategorie.ProbeerBepaal(inschrijving.Geboortedatum, start, out Leeftijdscategorie cat);
        Leeftijdsgroep? kandidaatGroep = kandidaatBuitenLeeftijd ? null : cat.Groep;

        var dagen = new List<VoorstelDagAnalyseDto>();
        foreach (Weekdag dag in Opvangdagen)
        {
            if (!inschrijving.OpenstaandeDagen.HasFlag(dag))
            {
                continue;
            }

            dagen.Add(BouwDag(dag, start, inschrijving, bezetting, vakantieLijst, doelStamgroep.MaxKinderen));
        }

        // De groepsgrootte-check op stamgroepniveau telt óók de voorlopige plaatsingen mee.
        int aantalBezet = bezetting.Count;

        return new VoorstelAnalyseDto(
            inschrijving.Id,
            $"{inschrijving.Voornaam} {inschrijving.Achternaam}",
            inschrijving.GewensteStartdatum,
            inschrijving.GewensteOpvangdagen,
            inschrijving.OpenstaandeDagen,
            inschrijving.Contracttype,
            doelStamgroep.Id,
            doelStamgroep.Naam,
            doelStamgroep.MaxKinderen,
            aantalBezet,
            doelStamgroep.HeeftPlaatsVoorExtraKind(aantalBezet),
            kandidaatBuitenLeeftijd,
            kandidaatGroep,
            voorlopig.Count,
            dagen);
    }

    private static VoorstelDagAnalyseDto BouwDag(
        Weekdag dag,
        DateOnly start,
        WachtlijstInschrijving inschrijving,
        IReadOnlyList<Kind> geplaatst,
        IReadOnlyList<Schoolvakantie> vakanties,
        int maxKinderen)
    {
        // De eerstvolgende kalenderdatum van deze weekdag op/na de startdatum.
        DateOnly peildatum = Plaatsing.EersteDatumOpWeekdag(dag, start);

        IReadOnlyList<Kind> aanwezig = Aanwezigheid.AanwezigOp(geplaatst, peildatum, vakanties);
        GroepSamenstelling samenstellingNu =
            GroepSamenstelling.VanafGeboortedata(aanwezig.Select(k => k.Geboortedatum), peildatum);

        (int? pmersNu, bool overschrijdtNu, string? meldingNu) = BerekenBkr(samenstellingNu);

        // De kandidaat-leeftijd op déze dag (kan net over een verjaardag heen liggen).
        int? pmersNa = null;
        bool overschrijdtNa = false;
        string? melding = meldingNu;
        if (!Leeftijdscategorie.ProbeerBepaal(inschrijving.Geboortedatum, peildatum, out Leeftijdscategorie cat))
        {
            melding = "Kind valt op deze dag buiten de opvangleeftijd (0-4 jaar).";
        }
        else
        {
            (pmersNa, overschrijdtNa, string? meldingNa) = BerekenBkr(samenstellingNu.MetExtra(cat.Groep));
            melding ??= meldingNa;
        }

        bool plekVrij = aanwezig.Count < maxKinderen;
        DateOnly? eersteVrije = plekVrij
            ? peildatum
            : Plaatsing.EersteVrijeDag(geplaatst, vakanties, dag, peildatum, maxKinderen);

        bool extraPmer = pmersNu is { } nu && pmersNa is { } na && na > nu;

        return new VoorstelDagAnalyseDto(
            dag,
            peildatum,
            aanwezig.Count,
            pmersNu,
            aanwezig.Count + 1,
            pmersNa,
            extraPmer,
            plekVrij,
            eersteVrije,
            overschrijdtNa,
            melding);
    }

    /// <summary>
    /// Berekent de BKR voor een samenstelling en vangt de overplanning
    /// (groep boven het wettelijk maximum) op als nette status i.p.v. een fout —
    /// analoog aan <see cref="Planning.WeekplanningBouwer"/>.
    /// </summary>
    private static (int? Pmers, bool Overschrijdt, string? Melding) BerekenBkr(GroepSamenstelling samenstelling)
    {
        try
        {
            BkrUitkomst uitkomst = BkrCalculator.Bereken(samenstelling);
            return (uitkomst.VereisteHoeveelheidPmers, false, null);
        }
        catch (GroepOverschrijdtMaximumException ex)
        {
            return (null, true, ex.Message);
        }
    }
}
