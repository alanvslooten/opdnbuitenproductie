namespace KinderKompas.Domain.Enums;

/// <summary>
/// Welke van de twee wettelijke rekenstappen uiteindelijk leidend was voor het
/// vereiste aantal pm'ers. De einduitkomst is altijd het MAXIMUM van beide
/// stappen (zie Besluit kwaliteit kinderopvang, Bijlage 1).
/// </summary>
public enum BkrStap
{
    /// <summary>Tabel 1 (leeftijdscategorie + groepstype) was leidend, of beide stappen gaven dezelfde uitkomst.</summary>
    Tabel1 = 0,

    /// <summary>Formule Z (baby-correctie) was leidend, want gaf een hogere uitkomst dan Tabel 1.</summary>
    FormuleZ = 1
}
