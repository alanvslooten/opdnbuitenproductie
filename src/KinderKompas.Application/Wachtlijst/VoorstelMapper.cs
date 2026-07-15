using KinderKompas.Domain.Entiteiten;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>Projecteert een <see cref="Voorstel"/> naar een <see cref="VoorstelDto"/> voor de historie.</summary>
public static class VoorstelMapper
{
    /// <param name="openstaandeDagenBijVersturen">
    /// De openstaande gewenste dagen op het moment van beoordelen; bepaalt of dit
    /// voorstel een deelvoorstel is (dekt het niet álle openstaande dagen).
    /// </param>
    public static VoorstelDto NaarDto(
        Voorstel voorstel, Domain.Enums.Weekdag openstaandeDagenBijVersturen, string? stamgroepNaam = null)
    {
        var dagen = voorstel.Dagen
            .OrderBy(d => d.VoorgesteldeDatum)
            .Select(d => new VoorstelDagDto(d.Weekdag, d.VoorgesteldeDatum))
            .ToList();

        return new VoorstelDto(
            voorstel.Id,
            voorstel.WachtlijstInschrijvingId,
            voorstel.VerstuurdOp,
            voorstel.VoorgesteldeStamgroepId,
            stamgroepNaam,
            voorstel.VoorgesteldeDagen,
            voorstel.IsDeelvoorstelVan(openstaandeDagenBijVersturen),
            voorstel.Status,
            voorstel.BeantwoordOp,
            voorstel.Notitie,
            dagen);
    }
}
