namespace KinderKompas.Domain.Enums;

/// <summary>
/// De soort verlof, met elk een eigen saldo. De exacte CAO-spelregels (opbouw,
/// vervaldatums, opname-volgorde) worden later aangeleverd; de structuur is hier
/// alvast configureerbaar (zie <c>Verlofsaldo</c>).
/// </summary>
public enum VerlofCategorie
{
    /// <summary>Wettelijke/bovenwettelijke vakantieuren.</summary>
    Vakantieuren = 0,

    /// <summary>Overig verlofbudget (bijv. compensatie-/duurzaam-inzetbaarheidsuren).</summary>
    Verlofbudget = 1
}
