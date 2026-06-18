using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Pure plaatsings-hulpfuncties voor de wachtlijst: bepaalt wanneer er in een
/// stamgroep op een bepaalde weekdag een plek vrijkomt. "Vrij" gaat hier over de
/// groepsbezetting (aantal aanwezige kinderen onder het groepsmaximum); de
/// wettelijke personeelseis (BKR) wordt apart berekend via de
/// <see cref="BkrCalculator"/>. Geen database- of UI-afhankelijkheid.
/// </summary>
public static class Plaatsing
{
    /// <summary>Standaard zoekhorizon voor het eerstvolgende vrije moment (~2 jaar).</summary>
    public const int StandaardMaxWekenVooruit = 104;

    /// <summary>
    /// Of er op de gegeven datum nog plaats is in de groep: het aantal volgens de
    /// planning aanwezige kinderen ligt onder <paramref name="maxKinderen"/>.
    /// </summary>
    public static bool IsPlekOp(
        IEnumerable<Kind> geplaatsteKinderen,
        DateOnly datum,
        IEnumerable<Schoolvakantie> vakanties,
        int maxKinderen)
    {
        ArgumentNullException.ThrowIfNull(geplaatsteKinderen);
        ArgumentNullException.ThrowIfNull(vakanties);
        return Aanwezigheid.AanwezigOp(geplaatsteKinderen, datum, vakanties).Count < maxKinderen;
    }

    /// <summary>
    /// De eerste datum vanaf <paramref name="vanaf"/> op de gegeven
    /// <paramref name="weekdag"/> waarop er plek is (bezetting onder het
    /// groepsmaximum). Geeft <c>null</c> als er binnen de zoekhorizon geen plek
    /// vrijkomt. Handig voor de voorstel-pop-up: "wanneer komt er een plek vrij?".
    /// </summary>
    public static DateOnly? EersteVrijeDag(
        IEnumerable<Kind> geplaatsteKinderen,
        IEnumerable<Schoolvakantie> vakanties,
        Weekdag weekdag,
        DateOnly vanaf,
        int maxKinderen,
        int maxWekenVooruit = StandaardMaxWekenVooruit)
    {
        ArgumentNullException.ThrowIfNull(geplaatsteKinderen);
        ArgumentNullException.ThrowIfNull(vakanties);
        if (!IsEnkeleOpvangdag(weekdag))
        {
            throw new ArgumentException(
                "Verwacht precies één opvang-weekdag (ma t/m vr).", nameof(weekdag));
        }

        // Materialiseer één keer; we doorlopen ze per kandidaatdatum.
        IReadOnlyList<Kind> kinderen =
            geplaatsteKinderen as IReadOnlyList<Kind> ?? geplaatsteKinderen.ToList();
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();

        DateOnly datum = EersteDatumOpWeekdag(weekdag, vanaf);
        for (int week = 0; week <= maxWekenVooruit; week++)
        {
            if (IsPlekOp(kinderen, datum, vakantieLijst, maxKinderen))
            {
                return datum;
            }

            datum = datum.AddDays(7);
        }

        return null;
    }

    /// <summary>De eerste datum op/na <paramref name="vanaf"/> die op de gegeven weekdag valt.</summary>
    public static DateOnly EersteDatumOpWeekdag(Weekdag weekdag, DateOnly vanaf)
    {
        DateOnly datum = vanaf;
        for (int i = 0; i < 7; i++)
        {
            if (Aanwezigheid.NaarWeekdag(datum) == weekdag)
            {
                return datum;
            }

            datum = datum.AddDays(1);
        }

        // Onbereikbaar: een geldige opvang-weekdag komt binnen 7 dagen voor.
        throw new InvalidOperationException($"Geen datum gevonden voor weekdag {weekdag}.");
    }

    /// <summary>Of de waarde precies één opvang-weekdag is (geen Geen, geen combinatie).</summary>
    private static bool IsEnkeleOpvangdag(Weekdag weekdag)
        => weekdag is Weekdag.Maandag or Weekdag.Dinsdag or Weekdag.Woensdag
            or Weekdag.Donderdag or Weekdag.Vrijdag;
}
