using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Het toegekende verlofsaldo van een medewerker per categorie, met een optionele
/// vervaldatum. De resterende uren = toegekend minus de goedgekeurde (en
/// openstaande) verlofaanvragen in dezelfde categorie; dat wordt afgeleid, niet
/// opgeslagen.
///
/// LET OP — CAO: de exacte opbouw-, opname- en vervalregels (bijv. wettelijke
/// vakantieuren vervallen 6 maanden na het opbouwjaar, bovenwettelijke na 5 jaar)
/// worden later aangeleverd. Tot die tijd is dit een configureerbare houder:
/// <see cref="ToegekendeUren"/> en <see cref="Vervaldatum"/> worden handmatig
/// gezet. Hier hoort straks de CAO-rekenregel.
/// </summary>
public class Verlofsaldo : TenantEntiteit
{
    public Guid MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }

    public VerlofCategorie Categorie { get; set; }

    /// <summary>Het toegekende aantal uren in deze categorie.</summary>
    public decimal ToegekendeUren { get; set; }

    /// <summary>
    /// Optionele vervaldatum van (een deel van) dit saldo. TODO: vervangen door de
    /// CAO-vervalregel zodra de exacte spelregels bekend zijn.
    /// </summary>
    public DateOnly? Vervaldatum { get; set; }
}
