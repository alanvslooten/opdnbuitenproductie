using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>
/// Gegevens van een (deel)plaatsing die is afgerond doordat de ouder een voorstel
/// accepteerde. Vormt de payload van het trigger-punt naar de to-do-afhandeling.
/// </summary>
public sealed record PlaatsingVoltooidGebeurtenis(
    Guid InschrijvingId,
    string KindNaam,
    Guid StamgroepId,
    Weekdag GeplaatsteDagen,
    DateOnly Startdatum,
    bool VolledigGeplaatst);

/// <summary>
/// Trigger-punt voor de vervolgactie ná acceptatie van een plaatsingsvoorstel:
/// er moet een to-do voor Gail ontstaan om het contract in Portabase op te maken.
/// De to-do zelf (met scherm, afvinken, herinnering) komt in fase 9; deze poort
/// is het contract waaraan die afhandeling straks wordt gekoppeld. Voor nu logt de
/// standaardimplementatie de gebeurtenis, zodat de flow end-to-end werkt zonder de
/// to-do-module al te bouwen.
/// </summary>
public interface IPlaatsingsToDo
{
    Task ContractOpmakenAsync(PlaatsingVoltooidGebeurtenis gebeurtenis, CancellationToken ct = default);
}
