namespace KinderKompas.Application.Abstractions;

/// <summary>
/// Abstractie over de opslag van binaire bestanden (zoals observatie-PDF's). De
/// Application-/Api-laag werkt alleen met sleutels en streams en weet niet wáár het
/// bestand staat. Lokaal is dat de schijf; in productie (Azure) wordt dit een
/// Blob Storage-implementatie, zonder dat de aanroepers wijzigen.
/// </summary>
public interface IBestandsopslag
{
    /// <summary>
    /// Slaat de inhoud op onder een nieuwe, unieke sleutel binnen de gegeven map
    /// (logische groepering, bijv. "observaties"). Geeft de sleutel terug waarmee het
    /// bestand later kan worden geopend of verwijderd.
    /// </summary>
    Task<string> OpslaanAsync(string map, string oorspronkelijkeBestandsnaam, Stream inhoud, CancellationToken ct = default);

    /// <summary>Opent het bestand als leesbare stream, of null als de sleutel niet bestaat.</summary>
    Task<Stream?> OpenenAsync(string sleutel, CancellationToken ct = default);

    /// <summary>Verwijdert het bestand. Geen fout als het al weg is (idempotent).</summary>
    Task VerwijderAsync(string sleutel, CancellationToken ct = default);
}
