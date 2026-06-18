using KinderKompas.Application.Meldingen;
using KinderKompas.Application.Wachtlijst;
using KinderKompas.Domain.Meldingen;

namespace KinderKompas.Infrastructure.Wachtlijst;

/// <summary>
/// De fase-9-invulling van het plaatsings-trigger-punt (verving de gelogde stub uit
/// fase 6): ná acceptatie van een voorstel ontstaat er een échte to-do in het
/// actiecentrum — "contract opmaken in Portabase" — door een
/// <see cref="PlaatsingGeaccepteerd"/>-event te publiceren. De wachtlijst-controller
/// blijft ongewijzigd: die kent alleen <see cref="IPlaatsingsToDo"/>.
/// </summary>
public sealed class MeldingPlaatsingsToDo : IPlaatsingsToDo
{
    private readonly IMeldingDispatcher _meldingen;

    public MeldingPlaatsingsToDo(IMeldingDispatcher meldingen)
    {
        _meldingen = meldingen;
    }

    public Task ContractOpmakenAsync(PlaatsingVoltooidGebeurtenis gebeurtenis, CancellationToken ct = default)
        => _meldingen.PubliceerAsync(
            new PlaatsingGeaccepteerd(
                gebeurtenis.InschrijvingId,
                gebeurtenis.KindNaam,
                gebeurtenis.Startdatum,
                gebeurtenis.VolledigGeplaatst),
            ct);
}
