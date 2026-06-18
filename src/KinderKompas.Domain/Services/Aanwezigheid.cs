using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Bepaalt — puur en deterministisch — of en welke kinderen op een gegeven dag
/// aanwezig zijn volgens de geplande opvang. Dit is INPUT voor het rooster, niet de
/// officiële aan-/afmelding (die blijft in het externe systeem).
///
/// Een kind is op een datum aanwezig als aan ÁLLE voorwaarden is voldaan:
///   1. de datum valt binnen [Startdatum, EffectieveEinddatum];
///   2. het kind is op die datum nog geen 4 jaar (binnen de opvangleeftijd 0-4);
///   3. de weekdag staat in de gewenste opvangdagen van het kind;
///   4. bij een 40-wekencontract: de datum valt NIET in een schoolvakantie.
///
/// Geen enkele afhankelijkheid van database of UI; volledig unit-testbaar.
/// </summary>
public static class Aanwezigheid
{
    /// <summary>Of de gegeven datum binnen één van de schoolvakanties valt.</summary>
    public static bool IsSchoolvakantie(DateOnly datum, IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(vakanties);
        foreach (Schoolvakantie vakantie in vakanties)
        {
            if (vakantie.Omvat(datum))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Of het opgegeven kind volgens de planning op de datum aanwezig is.</summary>
    public static bool IsKindAanwezigOp(
        Kind kind, DateOnly datum, IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(vakanties);

        // 1. Binnen de looptijd van het contract.
        if (datum < kind.Startdatum || datum > kind.EffectieveEinddatum)
        {
            return false;
        }

        // 2. Nog binnen de opvangleeftijd (op/na de 4e verjaardag telt het kind niet mee).
        if (!Leeftijdscategorie.ProbeerBepaal(kind.Geboortedatum, datum, out _))
        {
            return false;
        }

        // 3. Een gewenste opvangdag (weekend valt sowieso buiten de opvang).
        Weekdag dag = NaarWeekdag(datum);
        if (dag == Weekdag.Geen || !kind.GewensteOpvangdagen.HasFlag(dag))
        {
            return false;
        }

        // 4. 40-wekencontract wordt in schoolvakantieweken niet ingepland.
        if (kind.Contracttype == Contracttype.Weken40 && IsSchoolvakantie(datum, vakanties))
        {
            return false;
        }

        return true;
    }

    /// <summary>De kinderen uit de set die op de gegeven datum aanwezig zijn.</summary>
    public static IReadOnlyList<Kind> AanwezigOp(
        IEnumerable<Kind> kinderen, DateOnly datum, IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(kinderen);
        // Materialiseer de vakanties één keer; ze worden per kind doorlopen.
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();

        return kinderen.Where(k => IsKindAanwezigOp(k, datum, vakantieLijst)).ToList();
    }

    /// <summary>
    /// Bouwt de leeftijdsopbouw (<see cref="GroepSamenstelling"/>) van de aanwezige
    /// kinderen op de peildatum — de directe input voor de
    /// <see cref="BkrCalculator"/>. De leeftijdscategorie wordt per kind op de
    /// peildatum afgeleid.
    /// </summary>
    public static GroepSamenstelling SamenstellingOp(
        IEnumerable<Kind> kinderen, DateOnly datum, IEnumerable<Schoolvakantie> vakanties)
    {
        IReadOnlyList<Kind> aanwezig = AanwezigOp(kinderen, datum, vakanties);
        return GroepSamenstelling.VanafGeboortedata(aanwezig.Select(k => k.Geboortedatum), datum);
    }

    /// <summary>Vertaalt een kalenderdatum naar de opvang-weekdag (weekend = <see cref="Weekdag.Geen"/>).</summary>
    public static Weekdag NaarWeekdag(DateOnly datum) => datum.DayOfWeek switch
    {
        DayOfWeek.Monday => Weekdag.Maandag,
        DayOfWeek.Tuesday => Weekdag.Dinsdag,
        DayOfWeek.Wednesday => Weekdag.Woensdag,
        DayOfWeek.Thursday => Weekdag.Donderdag,
        DayOfWeek.Friday => Weekdag.Vrijdag,
        _ => Weekdag.Geen
    };
}
