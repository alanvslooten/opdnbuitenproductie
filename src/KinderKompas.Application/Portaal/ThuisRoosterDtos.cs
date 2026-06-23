using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Application.Portaal;

/// <summary>
/// Het eigen weekrooster van één medewerker in het thuis-portaal (fase 8). Bevat
/// BEWUST geen oudergegevens of gegevens van collega's: alleen de eigen diensten,
/// en alleen wanneer de planner de week heeft verstuurd.
/// </summary>
public sealed record ThuisRoosterDto(
    DateOnly WeekBegin,
    bool Verstuurd,
    DateTime? VerstuurdOp,
    IReadOnlyList<ThuisRoosterDagDto> Dagen);

/// <summary>Eén eigen dienstdag: groep, taak en urencorrectie. Geen privacygevoelige data.</summary>
public sealed record ThuisRoosterDagDto(
    DateOnly Datum,
    Weekdag Dag,
    Guid StamgroepId,
    string StamgroepNaam,
    string? Taakomschrijving,
    int UrencorrectieKwartieren,
    Dienstsoort Dienstsoort);

/// <summary>
/// Bouwt het eigen-rooster-leesmodel voor het thuis-portaal. Pure functie zonder
/// database/UI: krijgt de reeds op de eigen medewerker gefilterde diensten mee.
/// Is de week nog niet verstuurd, dan komen er bewust geen diensten terug — de
/// medewerker ziet z'n rooster pas na het versturen door de planner.
/// </summary>
public static class ThuisRoosterBouwer
{
    public static ThuisRoosterDto Bouw(
        DateOnly weekBegin,
        Roosterweek? week,
        IEnumerable<Roosterdienst> eigenDiensten,
        IReadOnlyDictionary<Guid, string> stamgroepNamen)
    {
        bool verstuurd = week is { Status: RoosterStatus.Verstuurd };

        if (!verstuurd)
        {
            return new ThuisRoosterDto(weekBegin, false, week?.VerstuurdOp, []);
        }

        List<ThuisRoosterDagDto> dagen = eigenDiensten
            .OrderBy(d => d.Datum)
            .ThenBy(d => stamgroepNamen.GetValueOrDefault(d.StamgroepId, ""))
            .Select(d => new ThuisRoosterDagDto(
                d.Datum,
                Aanwezigheid.NaarWeekdag(d.Datum),
                d.StamgroepId,
                stamgroepNamen.GetValueOrDefault(d.StamgroepId, ""),
                d.Taakomschrijving,
                d.UrencorrectieKwartieren,
                d.Dienstsoort))
            .ToList();

        return new ThuisRoosterDto(weekBegin, true, week!.VerstuurdOp, dagen);
    }
}
