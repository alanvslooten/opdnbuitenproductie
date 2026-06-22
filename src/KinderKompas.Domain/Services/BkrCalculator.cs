using System.Globalization;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Exceptions;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Berekent de wettelijk vereiste beroepskracht-kindratio (BKR) voor de DAGOPVANG
/// (kinderdagverblijf). Pure, deterministische functie zonder enige afhankelijkheid
/// van database of UI — alle logica leeft hier in het domein en is unit-getest.
///
/// Bron van alle regels en constanten: Besluit kwaliteit kinderopvang, Bijlage 1,
/// en de officiële rekentool op 1ratio.nl/bkr (versie juni 2026). Zie het document
/// "BKR Rekenregels — Technische analyse".
///
/// Wettelijke twee-staps-berekening (beide stappen verplicht):
///   Stap 1: Tabel 1 raadplegen op basis van leeftijdscategorie + groepstype.
///   Stap 2: Formule Z toepassen als er kinderen van 0-1 jaar in de groep zijn.
///   Einduitkomst = MAX(stap 1, stap 2).
///
/// NIET geïmplementeerd (bewust, staat op de roadmap):
///   - Buitenschoolse opvang (BSO): (A × 0,1) + (B × 0,083), op locatieniveau.
///   - Combinatiegroepen KDV/BSO.
///   Zie <see cref="UitbreidingsplekVoorBsoEnCombinatiegroepen"/>.
/// </summary>
public static class BkrCalculator
{
    // === Stap 1 — Tabel 1: stamgroepen met ÉÉN leeftijdscategorie ===
    // Maximaal aantal kinderen per pm'er (de ratio). Bron: Tabel 1, Bijlage 1.
    //   0-1 jaar: 1 pm = 3 kinderen, 2 pm = 6  -> 3 per pm'er
    //   1-2 jaar: 1 pm = 5 kinderen, 2 pm = 10 -> 5 per pm'er
    //   2-3 jaar: 1 pm = 8 kinderen, 2 pm = 16 -> 8 per pm'er
    //   3-4 jaar: 1 pm = 8 kinderen, 2 pm = 16 -> 8 per pm'er
    private const int RatioNulTotEen = 3;
    private const int RatioEenTotTwee = 5;
    private const int RatioTweeTotDrie = 8;
    private const int RatioDrieTotVier = 8;

    // Wettelijk maximum groepsgrootte bij één leeftijd. Bron: voetnoot Tabel 1
    // ("loopt door tot 4 pm'ers: max. 12 kinderen bij 0-1 jaar, max. 16 bij oudere leeftijden").
    private const int MaxGroepNulTotEen = 12;
    private const int MaxGroepOuder = 16;

    // === Stap 2 — Formule Z (baby-correctie) ===
    // Z = A + ((B + C + D) / 1,2), altijd NAAR BOVEN afgerond. Bron: Bijlage 1.
    //   A = aantal 0-1 / 3 ; B = aantal 1-2 / 5 ; C = aantal 2-3 / 6 ; D = aantal 3-4 / 8
    private const decimal ZNoemerA = 3m;
    private const decimal ZNoemerB = 5m;
    private const decimal ZNoemerC = 6m;
    private const decimal ZNoemerD = 8m;
    private const decimal ZDeelfactor = 1.2m;

    // === Voetnoot-sublimieten gemengde groepen ===
    // 0-3 groep: max. 8 kinderen van 0-1 jaar. Bron: voetnoot Tabel 1 gemengde groepen.
    private const int MaxBabysInGroep0Tot3 = 8;

    // LET OP — TODO (regel ontbreekt in bronstuk): de 0-4 gemengde groep kent een
    // sublimiet van "max. 3 of 5 kinderen van 0-1 jaar, afhankelijk van de totale
    // groepsgrootte". De omslagdrempel staat NIET in het brondocument en is daarom
    // bewust NIET geïmplementeerd (geen wet verzinnen). Zodra de exacte drempel
    // bekend is: hier toevoegen, analoog aan MaxBabysInGroep0Tot3.

    /// <summary>
    /// Berekent de vereiste BKR voor een groep op een peildatum (strikte berekening).
    /// Voert beide wettelijke stappen uit en neemt de hoogste uitkomst.
    /// </summary>
    /// <exception cref="GroepOverschrijdtMaximumException">
    /// Als de groep het wettelijk maximum aantal kinderen (of baby's) overschrijdt.
    /// </exception>
    public static BkrUitkomst Bereken(GroepSamenstelling samenstelling)
    {
        if (samenstelling.IsLeeg)
        {
            return BkrUitkomst.Leeg;
        }

        ValideerWettelijkMaximum(samenstelling);

        var stappen = new List<string>();

        // --- Stap 1: Tabel 1 ---
        int tabel1 = BerekenTabel1(samenstelling, stappen);

        // --- Stap 2: Formule Z ---
        // Wettelijk wordt Formule Z UITSLUITEND toegepast als er kinderen van 0-1 jaar
        // in de groep zijn (het is een baby-correctie). Zonder baby's telt Z niet mee;
        // anders zou de kleinere Z-deler (bijv. /6 voor 2-3 jaar tegenover ratio 8 in
        // Tabel 1) ten onrechte een hogere eis opleveren. Bron: Bijlage 1.
        decimal z = 0m;
        int zAfgerond = 0;
        if (samenstelling.BevatBabys)
        {
            z = BerekenFormuleZ(samenstelling);
            zAfgerond = (int)Math.Ceiling(z);
            stappen.Add(string.Format(
                CultureInfo.GetCultureInfo("nl-NL"),
                "Stap 2 — Formule Z = {0:0.###} → naar boven afgerond {1} pm'er(s) " +
                "(verplicht: er zijn kinderen van 0-1 jaar).",
                z, zAfgerond));
        }
        else
        {
            stappen.Add("Stap 2 — Formule Z niet van toepassing: geen kinderen van 0-1 jaar.");
        }

        // --- Einduitkomst: MAX van beide stappen ---
        int eind = Math.Max(tabel1, zAfgerond);
        BkrStap leidend = zAfgerond > tabel1 ? BkrStap.FormuleZ : BkrStap.Tabel1;
        stappen.Add(string.Format(
            CultureInfo.GetCultureInfo("nl-NL"),
            "Einduitkomst — hoogste van Tabel 1 ({0}) en Formule Z ({1}): {2} pm'er(s). Leidend: {3}.",
            tabel1, zAfgerond, eind, leidend == BkrStap.FormuleZ ? "Formule Z" : "Tabel 1"));

        return new BkrUitkomst
        {
            VereisteHoeveelheidPmers = eind,
            UitkomstTabel1 = tabel1,
            FormuleZ = z,
            UitkomstFormuleZ = zAfgerond,
            LeidendeStap = leidend,
            Stappen = stappen
        };
    }

    /// <summary>
    /// Het vereiste aantal pm'ers voor <paramref name="aantalKinderen"/> kinderen van
    /// ÉÉN leeftijdscategorie, puur op basis van de ratio uit Tabel 1 (naar boven
    /// afgerond). Anders dan <see cref="Bereken"/> kent deze methode GEEN groepsmaximum:
    /// het is de locatie-/snelrekenvariant ("hoeveel pm'ers heb ik nodig voor zoveel
    /// kinderen van deze leeftijd?"), waarbij dezelfde ratio blijft gelden ongeacht over
    /// hoeveel fysieke stamgroepen je ze verdeelt. Zo blijven de wettelijke ratio's op
    /// één plek in het domein staan en hoeft de UI/rekentool ze niet te dupliceren.
    /// </summary>
    public static int VereisteVoorEnkeleLeeftijd(Leeftijdsgroep groep, int aantalKinderen)
    {
        if (aantalKinderen < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(aantalKinderen),
                "Een aantal kinderen kan niet negatief zijn.");
        }

        return aantalKinderen == 0 ? 0 : CeilDeling(aantalKinderen, RatioVan(groep));
    }

    /// <summary>
    /// Drie-uursregeling: maximaal 3 uur per dag mag er minder personeel aanwezig zijn
    /// dan de strikte BKR vereist (typisch begin/eind van de dag en tijdens de lunch).
    /// Tijdens die afwijkuren geldt alleen de harde ondergrens: er moet ALTIJD minimaal
    /// 1 pm'er aanwezig zijn zolang er kinderen zijn. Bron: 3-uursregeling, Bijlage 1.
    ///
    /// De strikte BKR (<see cref="Bereken"/>) blijft altijd de basisberekening; deze
    /// methode geeft enkel de minimaal toegestane bezetting binnen het afwijkvenster.
    /// </summary>
    public static int MinimaleBezettingDriehursregeling(GroepSamenstelling samenstelling)
        => samenstelling.IsLeeg ? 0 : 1;

    /// <summary>
    /// Rekenregel 2: als het toevoegen van één kind door een afrondingsrandsituatie zou
    /// leiden tot een LAGER vereist aantal pm'ers, wordt dat aantal met 1 verhoogd.
    /// Het vereiste aantal mag immers nooit dalen door een kind erbij. Bron: Bijlage 1,
    /// rekenregel 2.
    /// </summary>
    /// <param name="vereistZonderExtraKind">Vereiste pm'ers vóór toevoegen van het kind.</param>
    /// <param name="vereistMetExtraKind">Vereiste pm'ers ná toevoegen van het kind.</param>
    /// <returns>Het gecorrigeerde vereiste aantal pm'ers voor de uitgebreide groep.</returns>
    public static int PasRekenregel2Toe(int vereistZonderExtraKind, int vereistMetExtraKind)
        => vereistMetExtraKind < vereistZonderExtraKind
            ? vereistMetExtraKind + 1
            : vereistMetExtraKind;

    // === Privé hulpfuncties ===

    private static int BerekenTabel1(GroepSamenstelling s, List<string> stappen)
    {
        if (s.IsEnkeleLeeftijd)
        {
            Leeftijdsgroep groep = s.AanwezigeGroepen[0];
            int ratio = RatioVan(groep);
            int pmers = CeilDeling(s.Totaal, ratio);
            stappen.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Stap 1 — Tabel 1 (stamgroep één leeftijd {0}, {1} kinderen, {2} per pm'er): {3} pm'er(s).",
                OmschrijfGroep(groep), s.Totaal, ratio, pmers));
            return pmers;
        }

        // Gemengde groep: bepaal de leeftijdsspan (laagste t/m hoogste aanwezige categorie).
        Leeftijdsgroep min = s.AanwezigeGroepen[0];
        Leeftijdsgroep max = s.AanwezigeGroepen[^1];
        (int max1pm, int max2pm) = GemengdeMaxima(min, max);

        int gemengdPmers = s.Totaal <= max1pm ? 1 : 2; // groepen groter dan 2pm-max zijn al afgevangen in validatie
        stappen.Add(string.Format(
            CultureInfo.InvariantCulture,
            "Stap 1 — Tabel 1 (gemengde groep {0}, {1} kinderen; max. {2} bij 1 pm'er, {3} bij 2 pm'ers): {4} pm'er(s).",
            OmschrijfSpan(min, max), s.Totaal, max1pm, max2pm, gemengdPmers));
        return gemengdPmers;
    }

    private static decimal BerekenFormuleZ(GroepSamenstelling s)
    {
        decimal a = s.AantalNulTotEen / ZNoemerA;
        decimal b = s.AantalEenTotTwee / ZNoemerB;
        decimal c = s.AantalTweeTotDrie / ZNoemerC;
        decimal d = s.AantalDrieTotVier / ZNoemerD;
        return a + (b + c + d) / ZDeelfactor;
    }

    private static void ValideerWettelijkMaximum(GroepSamenstelling s)
    {
        if (s.IsEnkeleLeeftijd)
        {
            Leeftijdsgroep groep = s.AanwezigeGroepen[0];
            int maxGroep = groep == Leeftijdsgroep.NulTotEen ? MaxGroepNulTotEen : MaxGroepOuder;
            if (s.Totaal > maxGroep)
            {
                throw new GroepOverschrijdtMaximumException(
                    $"Groep met alleen {OmschrijfGroep(groep)} heeft {s.Totaal} kinderen, " +
                    $"maar het wettelijk maximum is {maxGroep}.");
            }
            return;
        }

        Leeftijdsgroep min = s.AanwezigeGroepen[0];
        Leeftijdsgroep max = s.AanwezigeGroepen[^1];
        (int _, int max2pm) = GemengdeMaxima(min, max);
        if (s.Totaal > max2pm)
        {
            throw new GroepOverschrijdtMaximumException(
                $"Gemengde groep {OmschrijfSpan(min, max)} heeft {s.Totaal} kinderen, " +
                $"maar het wettelijk maximum is {max2pm}.");
        }

        // Sublimiet baby's in een 0-3 groep (span 0-1 t/m 2-3): max. 8 kinderen van 0-1.
        if (min == Leeftijdsgroep.NulTotEen && max == Leeftijdsgroep.TweeTotDrie &&
            s.AantalNulTotEen > MaxBabysInGroep0Tot3)
        {
            throw new GroepOverschrijdtMaximumException(
                $"Gemengde groep 0-3 jaar mag maximaal {MaxBabysInGroep0Tot3} kinderen van " +
                $"0-1 jaar bevatten, maar er zijn er {s.AantalNulTotEen}.");
        }
    }

    private static int RatioVan(Leeftijdsgroep groep) => groep switch
    {
        Leeftijdsgroep.NulTotEen => RatioNulTotEen,
        Leeftijdsgroep.EenTotTwee => RatioEenTotTwee,
        Leeftijdsgroep.TweeTotDrie => RatioTweeTotDrie,
        Leeftijdsgroep.DrieTotVier => RatioDrieTotVier,
        _ => throw new ArgumentOutOfRangeException(nameof(groep))
    };

    /// <summary>
    /// Tabel 1 — gemengde leeftijdsgroepen: (max. kinderen bij 1 pm'er, max. bij 2 pm'ers).
    /// Bron: Tabel 1 gemengde groepen, Bijlage 1. De tabel kent voor gemengde groepen
    /// uitsluitend de 1- en 2-pm'er-maxima; grotere groepen zijn wettelijk niet toegestaan
    /// en worden in de validatie afgevangen.
    /// </summary>
    private static (int Max1Pm, int Max2Pm) GemengdeMaxima(Leeftijdsgroep min, Leeftijdsgroep max)
        => (min, max) switch
        {
            (Leeftijdsgroep.NulTotEen, Leeftijdsgroep.EenTotTwee) => (4, 8),    // 0-2 jaar
            (Leeftijdsgroep.NulTotEen, Leeftijdsgroep.TweeTotDrie) => (5, 10),  // 0-3 jaar
            (Leeftijdsgroep.NulTotEen, Leeftijdsgroep.DrieTotVier) => (5, 12),  // 0-4 jaar
            (Leeftijdsgroep.EenTotTwee, Leeftijdsgroep.TweeTotDrie) => (6, 11), // 1-3 jaar
            (Leeftijdsgroep.EenTotTwee, Leeftijdsgroep.DrieTotVier) => (7, 13), // 1-4 jaar
            (Leeftijdsgroep.TweeTotDrie, Leeftijdsgroep.DrieTotVier) => (8, 16),// 2-4 jaar
            _ => throw new ArgumentOutOfRangeException(
                nameof(min), $"Onbekende gemengde leeftijdsspan {min}-{max}.")
        };

    private static int CeilDeling(int teller, int noemer) => (teller + noemer - 1) / noemer;

    private static string OmschrijfGroep(Leeftijdsgroep groep) => groep switch
    {
        Leeftijdsgroep.NulTotEen => "0-1 jaar",
        Leeftijdsgroep.EenTotTwee => "1-2 jaar",
        Leeftijdsgroep.TweeTotDrie => "2-3 jaar",
        Leeftijdsgroep.DrieTotVier => "3-4 jaar",
        _ => groep.ToString()
    };

    private static string OmschrijfSpan(Leeftijdsgroep min, Leeftijdsgroep max)
        => $"{(int)min}-{(int)max + 1} jaar";

    /// <summary>
    /// Bewust lege uitbreidingsplek. Hier komen later de BSO-berekening
    /// ((A × 0,1) + (B × 0,083), op locatieniveau per 1 juli 2024) en de
    /// combinatiegroepen KDV/BSO. Zie roadmap in het brondocument.
    /// </summary>
    internal static void UitbreidingsplekVoorBsoEnCombinatiegroepen()
    {
        // Opzettelijk niet geïmplementeerd in fase 2.
    }
}
