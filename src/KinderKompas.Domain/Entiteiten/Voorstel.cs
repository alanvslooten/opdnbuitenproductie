using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén plaatsingsvoorstel aan de ouder, onderdeel van de voorstelhistorie van een
/// <see cref="WachtlijstInschrijving"/>. Een voorstel kan een <em>deelvoorstel</em>
/// zijn: het stelt dan een subset van de gewenste dagen voor; de overige dagen
/// blijven op de wachtlijst. Per voorgestelde dag legt de planner een concrete
/// startdatum vast (zie <see cref="Dagen"/>).
/// </summary>
public class Voorstel : TenantEntiteit
{
    public Guid WachtlijstInschrijvingId { get; set; }
    public WachtlijstInschrijving? WachtlijstInschrijving { get; set; }

    /// <summary>Wanneer het voorstel naar de ouder is verstuurd.</summary>
    public DateTime VerstuurdOp { get; set; }

    /// <summary>De stamgroep waarin geplaatst zou worden.</summary>
    public Guid VoorgesteldeStamgroepId { get; set; }
    public Stamgroep? VoorgesteldeStamgroep { get; set; }

    /// <summary>De voorgestelde dagen (bit-vlaggen). Een subset = deelvoorstel.</summary>
    public Weekdag VoorgesteldeDagen { get; set; }

    public VoorstelStatus Status { get; set; } = VoorstelStatus.Verstuurd;

    /// <summary>Wanneer de ouder reageerde (accepteren/afwijzen), of null zolang verstuurd.</summary>
    public DateTime? BeantwoordOp { get; set; }

    /// <summary>Vrije toelichting bij het voorstel.</summary>
    public string? Notitie { get; set; }

    /// <summary>De concrete voorgestelde startdatum per voorgestelde dag (door de planner ingevuld).</summary>
    public ICollection<VoorstelDag> Dagen { get; set; } = new List<VoorstelDag>();

    /// <summary>
    /// Of dit een deelvoorstel is t.o.v. de gegeven (op het moment van versturen)
    /// openstaande gewenste dagen: het dekt dan niet álle openstaande dagen.
    /// </summary>
    public bool IsDeelvoorstelVan(Weekdag openstaandeDagen)
        => (VoorgesteldeDagen & openstaandeDagen) != openstaandeDagen;
}
