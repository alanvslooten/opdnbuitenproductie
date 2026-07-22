using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een bijlage (bestand) bij een <see cref="KennisbankDocument"/>: bijv. een PDF van
/// een protocol of het pedagogisch beleidsplan. De bytes staan buiten de database
/// (zie <c>IBestandsopslag</c>); deze entiteit legt alleen de metadata + opslagsleutel vast.
/// </summary>
public class KennisbankBijlage : TenantEntiteit
{
    public Guid KennisbankDocumentId { get; set; }
    public KennisbankDocument? Document { get; set; }

    /// <summary>De oorspronkelijke bestandsnaam (voor weergave en download).</summary>
    public required string BestandsNaam { get; set; }

    /// <summary>De opslagsleutel waarmee het bestand kan worden geopend/verwijderd.</summary>
    public required string BestandsSleutel { get; set; }

    /// <summary>Het MIME-type van het bestand (bijv. application/pdf).</summary>
    public required string ContentType { get; set; }

    /// <summary>Bestandsgrootte in bytes.</summary>
    public long BestandsGrootte { get; set; }
}
