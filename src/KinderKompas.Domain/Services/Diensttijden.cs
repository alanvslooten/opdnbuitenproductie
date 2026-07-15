using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Pure rekenregels rond diensttijden: de standaardtijden per <see cref="Dienstsoort"/>,
/// de (onbetaalde) pauze op basis van de dienstduur, en de netto geplande uren. De
/// standaardtijden zijn een vertrekpunt; per dienst mogen ze handmatig worden aangepast
/// (bij weinig kinderen bijvoorbeeld eerder stoppen). Geen database- of UI-afhankelijkheid.
/// </summary>
public static class Diensttijden
{
    /// <summary>Vanaf deze bruto dienstduur geldt de lange-dienst-pauze (1 uur i.p.v. 0,5 uur).</summary>
    public static readonly TimeSpan LangeDienstDrempel = TimeSpan.FromHours(6);

    private static readonly TimeSpan LangePauze = TimeSpan.FromHours(1);
    private static readonly TimeSpan KortePauze = TimeSpan.FromMinutes(30);

    /// <summary>De standaard begin- en eindtijd per dienstsoort (bron: Op d'n Buiten, v3).</summary>
    public static (TimeOnly Begin, TimeOnly Eind) Standaard(Dienstsoort soort) => soort switch
    {
        Dienstsoort.Vroege => (new TimeOnly(7, 30), new TimeOnly(17, 30)),
        Dienstsoort.Regulier => (new TimeOnly(8, 0), new TimeOnly(18, 0)),
        Dienstsoort.Late => (new TimeOnly(8, 30), new TimeOnly(18, 0)),
        _ => (new TimeOnly(8, 0), new TimeOnly(18, 0)),
    };

    /// <summary>
    /// De (onbetaalde) pauze voor een dienst van <paramref name="begin"/> tot
    /// <paramref name="eind"/>: 1 uur bij een lange dienst (≥ 6 uur bruto), anders 0,5 uur.
    /// Een niet-positieve duur levert geen pauze op.
    /// </summary>
    public static TimeSpan Pauze(TimeOnly begin, TimeOnly eind)
    {
        TimeSpan bruto = Brutoduur(begin, eind);
        if (bruto <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return bruto >= LangeDienstDrempel ? LangePauze : KortePauze;
    }

    /// <summary>De bruto dienstduur (eind − begin); 0 als de eindtijd niet ná de begintijd ligt.</summary>
    public static TimeSpan Brutoduur(TimeOnly begin, TimeOnly eind)
    {
        // Via ToTimeSpan() i.p.v. de TimeOnly-min-operator: die laatste wrapt rond de klok
        // (18:00 − 08:00 zou 14 uur geven), wat een eind-vóór-begin niet als 0 herkent.
        TimeSpan duur = eind.ToTimeSpan() - begin.ToTimeSpan();
        return duur > TimeSpan.Zero ? duur : TimeSpan.Zero;
    }

    /// <summary>
    /// De netto geplande uren van een dienst: bruto duur minus de onbetaalde pauze.
    /// Afgerond op kwartieren (net als de urenregistratie), nooit negatief.
    /// </summary>
    public static decimal NettoUren(TimeOnly begin, TimeOnly eind)
    {
        TimeSpan netto = Brutoduur(begin, eind) - Pauze(begin, eind);
        if (netto <= TimeSpan.Zero)
        {
            return 0m;
        }

        decimal kwartieren = Math.Round((decimal)netto.TotalMinutes / 15m, MidpointRounding.AwayFromZero);
        return kwartieren / 4m;
    }
}
