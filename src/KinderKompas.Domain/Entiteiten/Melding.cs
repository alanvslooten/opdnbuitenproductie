using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén item in het app-brede actiecentrum (fase 9): een melding of een af te vinken
/// to-do. Ontstaat uit een <see cref="Meldingen.MeldingGebeurtenis"/> (domein-event)
/// via de <see cref="Services.MeldingFabriek"/>; nooit rechtstreeks in een controller
/// opgebouwd. De inhoud is afgeleide weergave-tekst — de bron-van-waarheid blijft de
/// onderliggende module (zie <see cref="BronType"/>/<see cref="BronId"/> voor de deep-link).
/// </summary>
public class Melding : TenantEntiteit
{
    public MeldingSoort Soort { get; set; }

    public MeldingStatus Status { get; set; } = MeldingStatus.Ongelezen;

    /// <summary>
    /// Of dit een af te vinken to-do is (true) of een puur informatieve melding (false).
    /// Stuurt de "open to-do's"-telling en het afhandel-gedrag.
    /// </summary>
    public bool VereistActie { get; set; }

    /// <summary>Korte kop voor de lijst/het belletje.</summary>
    public required string Titel { get; set; }

    /// <summary>Toelichtende regel met de relevante details (naam, datum, actie).</summary>
    public required string Tekst { get; set; }

    /// <summary>Het type bronentiteit ("Verlofaanvraag", "WachtlijstInschrijving", …) voor de deep-link; optioneel.</summary>
    public string? BronType { get; set; }

    /// <summary>De id van de bronentiteit waarnaar de melding verwijst; optioneel.</summary>
    public Guid? BronId { get; set; }

    /// <summary>
    /// Idempotentiesleutel: triggers kunnen herhaaldelijk vuren (bv. dezelfde BKR-dag).
    /// De dispatcher gebruikt deze sleutel om een nog-niet-afgehandelde melding te
    /// hergebruiken i.p.v. het actiecentrum vol te spammen. Null = altijd nieuw.
    /// </summary>
    public string? DeduplicatieSleutel { get; set; }

    /// <summary>Tijdstip (UTC) waarop de to-do is afgevinkt, of null.</summary>
    public DateTime? AfgehandeldOp { get; set; }

    /// <summary>Markeer als gelezen (alleen vanuit ongelezen; afgehandeld blijft afgehandeld).</summary>
    public void MarkeerGelezen()
    {
        if (Status == MeldingStatus.Ongelezen)
        {
            Status = MeldingStatus.Gelezen;
        }
    }

    /// <summary>Vink een to-do af (idempotent).</summary>
    public void HandelAf(DateTime nu)
    {
        Status = MeldingStatus.Afgehandeld;
        AfgehandeldOp = nu;
    }

    /// <summary>Of dit een nog openstaande to-do is (actie vereist, nog niet afgehandeld).</summary>
    public bool IsOpenToDo => VereistActie && Status != MeldingStatus.Afgehandeld;
}
