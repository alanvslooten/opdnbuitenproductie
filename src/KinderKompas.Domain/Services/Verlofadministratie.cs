using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Pure, deterministische berekening van verlofsaldo-standen uit een toegekend
/// saldo en de verlofaanvragen van een medewerker. Geen database- of UI-
/// afhankelijkheid: de caller laadt het saldo en de aanvragen en geeft die hier door.
///
/// LET OP — CAO: de opbouw-, opname-volgorde- en vervalregels worden later
/// aangeleverd. Deze service rekent nu enkel "toegekend minus opgenomen"; de
/// CAO-rekenregel (bijv. eerst wettelijke uren opnemen, vervallen saldi negeren)
/// hoort hier later bij.
/// </summary>
public static class Verlofadministratie
{
    /// <summary>
    /// Berekent de stand van één saldo. Alleen aanvragen in dezelfde categorie tellen
    /// mee: goedgekeurde uren zijn "gebruikt", openstaande zijn "gereserveerd",
    /// afgekeurde tellen niet.
    /// </summary>
    public static Verlofsaldostand BerekenStand(
        Verlofsaldo saldo, IEnumerable<Verlofaanvraag> aanvragen)
    {
        ArgumentNullException.ThrowIfNull(saldo);
        ArgumentNullException.ThrowIfNull(aanvragen);

        decimal gebruikt = 0m;
        decimal gereserveerd = 0m;

        foreach (Verlofaanvraag aanvraag in aanvragen)
        {
            if (aanvraag.Categorie != saldo.Categorie)
            {
                continue;
            }

            switch (aanvraag.Status)
            {
                case VerlofStatus.Goedgekeurd:
                    gebruikt += aanvraag.AantalUren;
                    break;
                case VerlofStatus.Openstaand:
                    gereserveerd += aanvraag.AantalUren;
                    break;
                case VerlofStatus.Afgekeurd:
                default:
                    break;
            }
        }

        return new Verlofsaldostand(
            saldo.Categorie, saldo.ToegekendeUren, gebruikt, gereserveerd, saldo.Vervaldatum);
    }
}
