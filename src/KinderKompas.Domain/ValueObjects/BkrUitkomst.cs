using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// Het resultaat van een BKR-berekening voor één groep op één peildatum.
/// Bevat naast de einduitkomst ook de tussenwaarden en een leesbare stap-voor-stap-
/// uitleg, zodat de berekening transparant getoond/geprint kan worden voor
/// dossiervorming en inspectie (GGD).
/// </summary>
public sealed record BkrUitkomst
{
    /// <summary>Het wettelijk vereiste minimum aantal pedagogisch medewerkers (de einduitkomst).</summary>
    public required int VereisteHoeveelheidPmers { get; init; }

    /// <summary>Uitkomst van stap 1 (Tabel 1): minimum aantal pm'ers op basis van leeftijd en groepstype.</summary>
    public required int UitkomstTabel1 { get; init; }

    /// <summary>De ruwe, niet-afgeronde waarde van Formule Z (0 als er geen baby's zijn).</summary>
    public required decimal FormuleZ { get; init; }

    /// <summary>Uitkomst van stap 2 (Formule Z), altijd naar boven afgerond.</summary>
    public required int UitkomstFormuleZ { get; init; }

    /// <summary>Welke stap leidend was voor de einduitkomst.</summary>
    public required BkrStap LeidendeStap { get; init; }

    /// <summary>Leesbare, stap-voor-stap-onderbouwing van de berekening (voor transparantie/dossier).</summary>
    public required IReadOnlyList<string> Stappen { get; init; }

    /// <summary>Een lege groep vereist geen pm'ers.</summary>
    public static BkrUitkomst Leeg { get; } = new()
    {
        VereisteHoeveelheidPmers = 0,
        UitkomstTabel1 = 0,
        FormuleZ = 0m,
        UitkomstFormuleZ = 0,
        LeidendeStap = BkrStap.Tabel1,
        Stappen = new[] { "Lege groep: geen kinderen aanwezig, dus 0 pm'ers vereist." }
    };
}
