using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// De berekende stand van één verlofsaldo (per medewerker per categorie) op een
/// peilmoment: hoeveel is toegekend, hoeveel is via goedgekeurd verlof gebruikt,
/// en hoeveel staat nog open (gereserveerd). Afgeleide, onveranderlijke momentopname.
/// </summary>
public readonly record struct Verlofsaldostand(
    VerlofCategorie Categorie,
    decimal Toegekend,
    decimal Gebruikt,
    decimal Gereserveerd,
    DateOnly? Vervaldatum)
{
    /// <summary>Resterende uren na het goedgekeurde verlof (openstaand telt hier niet mee).</summary>
    public decimal Resterend => Toegekend - Gebruikt;

    /// <summary>Resterende uren als ook de openstaande aanvragen zouden worden goedgekeurd.</summary>
    public decimal ResterendNaReservering => Toegekend - Gebruikt - Gereserveerd;
}
