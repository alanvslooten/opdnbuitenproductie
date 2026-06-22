using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Portaal;

/// <summary>Gewerkte uren per week binnen het overzicht.</summary>
public sealed record UrenWeekDto(DateOnly WeekBegin, decimal GewerkteUren, int AantalSessies);

/// <summary>
/// Urenoverzicht over een periode: totaal gewerkte (geklokte) uren, de verwachte uren
/// op basis van het contract, en het saldo meer-/minderuren. Gedeeld door het
/// back-office medewerkerdossier (F-22) en het thuis-portaal (eigen uren).
/// </summary>
public sealed record UrenoverzichtDto(
    DateOnly Van,
    DateOnly Tot,
    decimal GewerkteUren,
    decimal VerwachteUren,
    decimal MeerMinderUren,
    int AantalSessies,
    IReadOnlyList<UrenWeekDto> PerWeek);

/// <summary>
/// Bouwt — puur en testbaar — het urenoverzicht uit afgesloten urenregistraties. De
/// verwachte uren = contracturen/week × het aantal weken in de periode; meer-/minderuren
/// = gewerkt − verwacht. Open (nog niet uitgeklokte) registraties tellen niet mee.
/// </summary>
public static class UrenoverzichtBouwer
{
    public static UrenoverzichtDto Bouw(
        IEnumerable<Urenregistratie> registraties,
        decimal contracturenPerWeek,
        DateOnly van,
        DateOnly tot)
    {
        ArgumentNullException.ThrowIfNull(registraties);

        List<Urenregistratie> afgesloten = registraties.Where(u => !u.IsOpen).ToList();
        decimal gewerkt = afgesloten.Sum(u => u.GewerkteUren);

        int dagen = Math.Max(1, tot.DayNumber - van.DayNumber + 1);
        decimal weken = dagen / 7m;
        decimal verwacht = Math.Round(contracturenPerWeek * weken, 1, MidpointRounding.AwayFromZero);
        gewerkt = Math.Round(gewerkt, 2, MidpointRounding.AwayFromZero);

        List<UrenWeekDto> perWeek = afgesloten
            .GroupBy(u => WeekBeginVan(u.Datum))
            .Select(g => new UrenWeekDto(
                g.Key,
                Math.Round(g.Sum(x => x.GewerkteUren), 2, MidpointRounding.AwayFromZero),
                g.Count()))
            .OrderBy(w => w.WeekBegin)
            .ToList();

        return new UrenoverzichtDto(
            van, tot, gewerkt, verwacht,
            Math.Round(gewerkt - verwacht, 2, MidpointRounding.AwayFromZero),
            afgesloten.Count, perWeek);
    }

    private static DateOnly WeekBeginVan(DateOnly d)
    {
        int offset = ((int)d.DayOfWeek + 6) % 7; // ma=0
        return d.AddDays(-offset);
    }
}
