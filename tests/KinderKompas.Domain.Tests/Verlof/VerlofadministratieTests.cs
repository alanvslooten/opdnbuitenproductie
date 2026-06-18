using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Tests.Verlof;

/// <summary>
/// Bewijst de verlofsaldo-berekening (fase 5b): alleen goedgekeurd verlof telt als
/// "gebruikt", openstaande aanvragen als "gereserveerd", afgekeurde tellen niet, en
/// aanvragen uit een andere categorie blijven buiten beschouwing.
/// </summary>
public class VerlofadministratieTests
{
    private static readonly Guid Medewerker = Guid.NewGuid();

    private static Verlofsaldo Saldo(VerlofCategorie categorie, decimal toegekend) => new()
    {
        MedewerkerId = Medewerker,
        Categorie = categorie,
        ToegekendeUren = toegekend,
    };

    private static Verlofaanvraag Aanvraag(VerlofCategorie categorie, VerlofStatus status, decimal uren) => new()
    {
        MedewerkerId = Medewerker,
        Categorie = categorie,
        Status = status,
        AantalUren = uren,
        Begindatum = new DateOnly(2026, 7, 1),
        Einddatum = new DateOnly(2026, 7, 5),
    };

    [Fact]
    public void Goedgekeurd_telt_als_gebruikt_en_verlaagt_resterend()
    {
        Verlofsaldo saldo = Saldo(VerlofCategorie.Vakantieuren, 100m);
        var aanvragen = new[]
        {
            Aanvraag(VerlofCategorie.Vakantieuren, VerlofStatus.Goedgekeurd, 24m),
        };

        Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);

        Assert.Equal(24m, stand.Gebruikt);
        Assert.Equal(0m, stand.Gereserveerd);
        Assert.Equal(76m, stand.Resterend);
        Assert.Equal(76m, stand.ResterendNaReservering);
    }

    [Fact]
    public void Openstaand_telt_als_gereserveerd_maar_niet_als_gebruikt()
    {
        Verlofsaldo saldo = Saldo(VerlofCategorie.Vakantieuren, 100m);
        var aanvragen = new[]
        {
            Aanvraag(VerlofCategorie.Vakantieuren, VerlofStatus.Goedgekeurd, 20m),
            Aanvraag(VerlofCategorie.Vakantieuren, VerlofStatus.Openstaand, 16m),
        };

        Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);

        Assert.Equal(20m, stand.Gebruikt);
        Assert.Equal(16m, stand.Gereserveerd);
        Assert.Equal(80m, stand.Resterend);
        Assert.Equal(64m, stand.ResterendNaReservering);
    }

    [Fact]
    public void Afgekeurd_telt_niet_mee()
    {
        Verlofsaldo saldo = Saldo(VerlofCategorie.Vakantieuren, 50m);
        var aanvragen = new[]
        {
            Aanvraag(VerlofCategorie.Vakantieuren, VerlofStatus.Afgekeurd, 40m),
        };

        Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);

        Assert.Equal(0m, stand.Gebruikt);
        Assert.Equal(0m, stand.Gereserveerd);
        Assert.Equal(50m, stand.Resterend);
    }

    [Fact]
    public void Andere_categorie_telt_niet_mee_op_dit_saldo()
    {
        Verlofsaldo saldo = Saldo(VerlofCategorie.Vakantieuren, 80m);
        var aanvragen = new[]
        {
            Aanvraag(VerlofCategorie.Verlofbudget, VerlofStatus.Goedgekeurd, 30m),
            Aanvraag(VerlofCategorie.Vakantieuren, VerlofStatus.Goedgekeurd, 8m),
        };

        Verlofsaldostand stand = Verlofadministratie.BerekenStand(saldo, aanvragen);

        Assert.Equal(8m, stand.Gebruikt);
        Assert.Equal(72m, stand.Resterend);
    }
}
