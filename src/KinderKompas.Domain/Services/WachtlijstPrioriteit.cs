using System.Globalization;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Berekent — puur en deterministisch — de prioriteitsscore van een
/// wachtlijst-inschrijving op een peildatum. De score bepaalt (na de handmatige
/// "bovenaan"-kinderen) de volgorde op de wachtlijst.
///
/// <para>De gewichten zijn nu vaste, benoemde constanten. In fase 9 worden ze
/// configureerbaar via de instellingen; de berekening zelf blijft dan hier in het
/// domein. Bewust GEEN magische getallen: elk gewicht is benoemd en toegelicht.</para>
///
/// <para>Het puntensysteem (hoger = eerder aan de beurt):</para>
/// <list type="bullet">
///   <item>Interne aanvraag (broertje/zusje of doorstroom): vaste bonus.</item>
///   <item>Anciënniteit: punten per volledige maand dat het kind al op de
///   wachtlijst staat.</item>
/// </list>
/// <para>Een handmatig bovenaan gezet kind (personeelskind) staat los van de score
/// en gaat altijd vóór; dat wordt in de sortering (Application-laag) afgehandeld,
/// niet in de score.</para>
/// </summary>
public static class WachtlijstPrioriteit
{
    /// <summary>Bonuspunten voor een interne aanvraag (broertje/zusje, doorstroom).</summary>
    public const int PuntenIntern = 500;

    /// <summary>Punten per volledige maand dat het kind al op de wachtlijst staat (anciënniteit).</summary>
    public const int PuntenPerMaandWachtend = 10;

    /// <summary>Berekent de score met de standaardgewichten (code-defaults).</summary>
    public static WachtlijstPrioriteitsuitkomst Bereken(WachtlijstInschrijving inschrijving, DateOnly peildatum)
        => Bereken(inschrijving, peildatum, WachtlijstPrioriteitsgewichten.Standaard);

    /// <summary>
    /// Berekent de score met door de Beheerder ingestelde gewichten (fase 9c). De
    /// regels blijven gelijk; alleen de puntenwaarden zijn instelbaar.
    /// </summary>
    public static WachtlijstPrioriteitsuitkomst Bereken(
        WachtlijstInschrijving inschrijving, DateOnly peildatum, WachtlijstPrioriteitsgewichten gewichten)
    {
        ArgumentNullException.ThrowIfNull(inschrijving);
        ArgumentNullException.ThrowIfNull(gewichten);

        var onderdelen = new List<string>();
        int score = 0;

        if (inschrijving.IsIntern)
        {
            score += gewichten.PuntenIntern;
            onderdelen.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Interne aanvraag (broertje/zusje of doorstroom): +{0} punten.", gewichten.PuntenIntern));
        }

        int maanden = VolledigeMaanden(inschrijving.InschrijfdatumWachtlijst, peildatum);
        int ancienniteit = maanden * gewichten.PuntenPerMaandWachtend;
        score += ancienniteit;
        onderdelen.Add(string.Format(
            CultureInfo.InvariantCulture,
            "Anciënniteit: {0} volledige maand(en) op de wachtlijst × {1} = +{2} punten.",
            maanden, gewichten.PuntenPerMaandWachtend, ancienniteit));

        if (inschrijving.HandmatigBovenaan)
        {
            onderdelen.Add("Handmatig bovenaan gezet (bijv. personeelskind): gaat vóór op de score.");
        }

        return new WachtlijstPrioriteitsuitkomst
        {
            Score = score,
            HandmatigBovenaan = inschrijving.HandmatigBovenaan,
            Onderdelen = onderdelen
        };
    }

    /// <summary>
    /// Het aantal volledige maanden tussen twee datums (nooit negatief). Een kind
    /// dat in de toekomst is ingeschreven levert 0 maanden anciënniteit.
    /// </summary>
    private static int VolledigeMaanden(DateOnly van, DateOnly tot)
    {
        if (tot <= van)
        {
            return 0;
        }

        int maanden = (tot.Year - van.Year) * 12 + (tot.Month - van.Month);
        if (tot.Day < van.Day)
        {
            maanden--;
        }

        return Math.Max(0, maanden);
    }
}
