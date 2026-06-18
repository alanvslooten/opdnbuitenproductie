using KinderKompas.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace KinderKompas.Infrastructure.Bestandsopslag;

/// <summary>
/// Slaat bestanden op de lokale schijf op, onder een configureerbare hoofdmap.
/// De opgeslagen sleutel is een relatief pad (met forward slashes) binnen die map.
/// Beschermt tegen path-traversal: een sleutel kan nooit buiten de hoofdmap wijzen.
/// </summary>
public sealed class LokaleBestandsopslag : IBestandsopslag
{
    private readonly string _root;

    public LokaleBestandsopslag(IOptions<BestandsopslagOptions> opties)
    {
        string? geconfigureerd = opties.Value.Root;
        _root = string.IsNullOrWhiteSpace(geconfigureerd)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "bestanden")
            : geconfigureerd;
    }

    public async Task<string> OpslaanAsync(
        string map, string oorspronkelijkeBestandsnaam, Stream inhoud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inhoud);

        string veiligeMap = SaneerMap(map);
        string extensie = Path.GetExtension(oorspronkelijkeBestandsnaam);
        // Unieke bestandsnaam: voorkomt overschrijven en lekt geen oorspronkelijke naam.
        string bestandsnaam = $"{Guid.NewGuid():N}{extensie}";
        string relatieveSleutel = $"{veiligeMap}/{bestandsnaam}";

        string volledigPad = VeiligPad(relatieveSleutel);
        Directory.CreateDirectory(Path.GetDirectoryName(volledigPad)!);

        await using FileStream doel = File.Create(volledigPad);
        await inhoud.CopyToAsync(doel, ct);

        return relatieveSleutel;
    }

    public Task<Stream?> OpenenAsync(string sleutel, CancellationToken ct = default)
    {
        string volledigPad = VeiligPad(sleutel);
        if (!File.Exists(volledigPad))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(volledigPad);
        return Task.FromResult<Stream?>(stream);
    }

    public Task VerwijderAsync(string sleutel, CancellationToken ct = default)
    {
        string volledigPad = VeiligPad(sleutel);
        if (File.Exists(volledigPad))
        {
            File.Delete(volledigPad);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Zet een sleutel om naar een absoluut pad en bewaakt dat het binnen de hoofdmap
    /// blijft (geen "../"-uitbraak).
    /// </summary>
    private string VeiligPad(string sleutel)
    {
        if (string.IsNullOrWhiteSpace(sleutel))
        {
            throw new ArgumentException("Bestandssleutel mag niet leeg zijn.", nameof(sleutel));
        }

        string rootVolledig = Path.GetFullPath(_root);
        string doelVolledig = Path.GetFullPath(Path.Combine(rootVolledig, sleutel));

        string genormaliseerdeRoot =
            rootVolledig.EndsWith(Path.DirectorySeparatorChar) ? rootVolledig : rootVolledig + Path.DirectorySeparatorChar;

        if (!doelVolledig.StartsWith(genormaliseerdeRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ongeldige bestandssleutel (buiten de opslagmap).");
        }

        return doelVolledig;
    }

    private static string SaneerMap(string map) =>
        string.IsNullOrWhiteSpace(map) ? "overig" : map.Trim('/', '\\', ' ');
}
