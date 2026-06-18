using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een afgeronde observatie van een kind voor één observatiemoment (mijlpaal).
/// Het bestaan van deze rij betekent dat het moment is "afgevinkt"; verwijderen
/// maakt dat ongedaan (v1-bug: afvinken was onomkeerbaar — hier dus wél omkeerbaar).
///
/// De observatie zelf is een ge-uploade PDF (uit Piramide), GEEN in-app teksteditor.
/// De PDF-bytes staan buiten de database (zie <c>IBestandsopslag</c>); deze entiteit
/// houdt alleen de metadata en de verzendstatus richting de ouder bij.
/// </summary>
public class Observatie : TenantEntiteit
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    /// <summary>
    /// De mijlpaal (leeftijd in maanden) die deze observatie afdekt, bijv. 6, 24 of 46.
    /// Verwijst naar een moment uit <c>Observatieschema.Momenten</c>.
    /// </summary>
    public int MijlpaalMaanden { get; set; }

    /// <summary>Oorspronkelijke bestandsnaam van de geüploade PDF (voor weergave/download).</summary>
    public required string BestandsNaam { get; set; }

    /// <summary>De sleutel waarmee de PDF in de bestandsopslag is opgeslagen.</summary>
    public required string BestandsSleutel { get; set; }

    /// <summary>MIME-type van het bestand (verwacht: application/pdf).</summary>
    public required string ContentType { get; set; }

    /// <summary>Bestandsgrootte in bytes.</summary>
    public long BestandsGrootte { get; set; }

    /// <summary>
    /// Tijdstip (UTC) waarop de observatie naar de ouder is verstuurd, of null als ze
    /// nog niet verstuurd is. De daadwerkelijke mailverzending is in deze fase een stub.
    /// </summary>
    public DateTime? VerzondenOp { get; set; }

    /// <summary>Het e-mailadres waarnaar verstuurd is (vastgelegd op het moment van versturen).</summary>
    public string? VerzondenNaarEmail { get; set; }

    /// <summary>Of de observatie al naar de ouder is verstuurd.</summary>
    public bool IsVerzonden => VerzondenOp is not null;
}
