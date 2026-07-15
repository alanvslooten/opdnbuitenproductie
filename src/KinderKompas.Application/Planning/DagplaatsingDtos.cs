using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Planning;

/// <summary>Leesmodel van één dagafwijking (dagplaatsing) van een kind.</summary>
public sealed record DagplaatsingDto(
    Guid Id,
    Guid KindId,
    string KindVoornaam,
    string KindAchternaam,
    DateOnly Datum,
    Guid? StamgroepId,
    string? StamgroepNaam,
    DagplaatsingSoort Soort,
    string? Notitie,
    bool IsAanwezig);

/// <summary>
/// Invoer voor het zetten van een dagafwijking: het kind staat op <see cref="Datum"/> op
/// <see cref="StamgroepId"/> (of is afwezig als die null is). Per (kind, datum) bestaat er
/// hooguit één afwijking — een tweede zet op dezelfde dag overschrijft de eerste.
/// </summary>
public sealed record DagplaatsingInvoer(
    Guid KindId,
    DateOnly Datum,
    Guid? StamgroepId,
    DagplaatsingSoort Soort,
    string? Notitie);

/// <summary>Projecteert een <see cref="Dagplaatsing"/> naar zijn leesmodel.</summary>
public static class DagplaatsingMapper
{
    public static DagplaatsingDto NaarDto(Dagplaatsing d, Kind kind, string? stamgroepNaam) =>
        new(
            d.Id,
            d.KindId,
            kind.Voornaam,
            kind.Achternaam,
            d.Datum,
            d.StamgroepId,
            stamgroepNaam,
            d.Soort,
            d.Notitie,
            d.IsAanwezig);
}
