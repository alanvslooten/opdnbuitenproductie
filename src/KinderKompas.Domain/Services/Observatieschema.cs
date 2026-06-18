using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Berekent — puur en deterministisch — welke observatiemomenten een kind heeft en
/// wat hun status is op een peildatum. Kernregel (gewijzigd t.o.v. v1): de momenten
/// hangen aan de LEEFTIJD op basis van de GEBOORTEDATUM (vaste mijlpalen), niet aan
/// "laatste observatie + 6 maanden".
///
/// De reguliere momenten vallen elke 6 maanden van 6 t/m 42 maanden. Daarna volgt
/// géén 48-maandenmoment (het kind stopt op zijn 4e verjaardag), maar één bijzonder
/// EINDMOMENT op 46 maanden — 3 jaar en 10 maanden, ~2 maanden vóór de 4e verjaardag.
///
/// Geen enkele afhankelijkheid van database of UI; volledig unit-testbaar.
/// </summary>
public static class Observatieschema
{
    /// <summary>Interval tussen de reguliere momenten, in maanden.</summary>
    public const int IntervalMaanden = 6;

    /// <summary>Laatste reguliere mijlpaal (3,5 jaar). Daarna volgt het eindmoment.</summary>
    public const int LaatsteReguliereMaanden = 42;

    /// <summary>Het eindmoment: 3 jaar en 10 maanden (46 maanden).</summary>
    public const int EindmomentMaanden = 46;

    /// <summary>
    /// Standaarddrempel (in dagen) waarbinnen een naderend moment als
    /// <see cref="ObservatieStatus.Binnenkort"/> geldt. Later instelbaar via
    /// organisatie-instellingen (fase 9).
    /// </summary>
    public const int StandaardBinnenkortDrempelDagen = 30;

    /// <summary>
    /// De vaste catalogus van observatiemomenten, oplopend op leeftijd:
    /// 6, 12, 18, 24, 30, 36, 42 maanden en het eindmoment op 46 maanden.
    /// </summary>
    public static IReadOnlyList<Observatiemoment> Momenten { get; } = BouwMomenten();

    private static IReadOnlyList<Observatiemoment> BouwMomenten()
    {
        var lijst = new List<Observatiemoment>();
        for (int maanden = IntervalMaanden; maanden <= LaatsteReguliereMaanden; maanden += IntervalMaanden)
        {
            lijst.Add(new Observatiemoment(maanden, isEindmoment: false));
        }

        lijst.Add(new Observatiemoment(EindmomentMaanden, isEindmoment: true));
        return lijst;
    }

    /// <summary>De vervaldatum van een mijlpaal: de geboortedatum plus de mijlpaal in maanden.</summary>
    public static DateOnly VervaldatumVan(DateOnly geboortedatum, int mijlpaalMaanden)
        => geboortedatum.AddMonths(mijlpaalMaanden);

    /// <summary>
    /// Bepaalt voor elk observatiemoment de vervaldatum en status, gegeven de
    /// geboortedatum, de peildatum en de set reeds afgevinkte mijlpalen.
    /// </summary>
    /// <param name="geboortedatum">Geboortedatum van het kind.</param>
    /// <param name="peildatum">Datum waarop de status wordt bepaald (meestal vandaag).</param>
    /// <param name="afgerondeMijlpalen">
    /// De mijlpalen (in maanden) die al zijn afgevinkt/afgerond. Mag leeg zijn.
    /// </param>
    /// <param name="binnenkortDrempelDagen">
    /// Hoeveel dagen vooruit een naderend moment als "binnenkort" telt (default 30).
    /// </param>
    /// <returns>De momenten met status, oplopend op leeftijd.</returns>
    public static IReadOnlyList<ObservatiemomentStatus> Bereken(
        DateOnly geboortedatum,
        DateOnly peildatum,
        IReadOnlySet<int> afgerondeMijlpalen,
        int binnenkortDrempelDagen = StandaardBinnenkortDrempelDagen)
    {
        ArgumentNullException.ThrowIfNull(afgerondeMijlpalen);
        if (binnenkortDrempelDagen < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(binnenkortDrempelDagen), binnenkortDrempelDagen,
                "De binnenkort-drempel mag niet negatief zijn.");
        }

        var resultaat = new List<ObservatiemomentStatus>(Momenten.Count);
        foreach (Observatiemoment moment in Momenten)
        {
            DateOnly vervaldatum = VervaldatumVan(geboortedatum, moment.MijlpaalMaanden);
            bool afgerond = afgerondeMijlpalen.Contains(moment.MijlpaalMaanden);
            ObservatieStatus status =
                BepaalStatus(vervaldatum, peildatum, afgerond, binnenkortDrempelDagen);
            resultaat.Add(new ObservatiemomentStatus(moment, vervaldatum, status));
        }

        return resultaat;
    }

    private static ObservatieStatus BepaalStatus(
        DateOnly vervaldatum, DateOnly peildatum, bool afgerond, int drempelDagen)
    {
        if (afgerond)
        {
            return ObservatieStatus.Afgerond;
        }

        if (vervaldatum < peildatum)
        {
            return ObservatieStatus.Overschreden;
        }

        // Vandaag t/m de drempel telt als "binnenkort": het moment kan nu opgepakt worden.
        if (vervaldatum <= peildatum.AddDays(drempelDagen))
        {
            return ObservatieStatus.Binnenkort;
        }

        return ObservatieStatus.NogNietAanDeBeurt;
    }
}

/// <summary>
/// Eén observatiemoment voor een concreet kind op een peildatum: de
/// <see cref="Observatiemoment"/>-definitie, de berekende vervaldatum en de status.
/// </summary>
public readonly record struct ObservatiemomentStatus(
    Observatiemoment Moment,
    DateOnly Vervaldatum,
    ObservatieStatus Status);
