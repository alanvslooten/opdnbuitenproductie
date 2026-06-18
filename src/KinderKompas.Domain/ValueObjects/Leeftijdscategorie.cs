using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// Onveranderlijk begrip: de wettelijke leeftijdscategorie van een kind op een
/// bepaalde peildatum. Wordt altijd AFGELEID uit geboortedatum + peildatum en
/// nooit opgeslagen — een kind verandert vanzelf van categorie als het ouder
/// wordt. De fysieke stamgroep staat hier expliciet los van.
/// </summary>
public readonly record struct Leeftijdscategorie
{
    /// <summary>De wettelijke leeftijdsgroep (0-1, 1-2, 2-3, 3-4).</summary>
    public Leeftijdsgroep Groep { get; }

    /// <summary>Volledige levensjaren van het kind op de peildatum (0 t/m 3).</summary>
    public int VolledigeJaren { get; }

    private Leeftijdscategorie(Leeftijdsgroep groep, int volledigeJaren)
    {
        Groep = groep;
        VolledigeJaren = volledigeJaren;
    }

    /// <summary>
    /// Bepaalt de leeftijdscategorie van een kind op de gegeven peildatum.
    /// Geldig voor kinderen van 0 tot (net niet) 4 jaar — de doelgroep van de
    /// kinderopvang en van de BKR. Een kind dat op de peildatum 4 jaar of ouder
    /// is, of nog niet geboren, valt buiten deze schaal en levert een fout op.
    /// </summary>
    /// <param name="geboortedatum">Geboortedatum van het kind.</param>
    /// <param name="peildatum">Datum waarop de leeftijd wordt bepaald.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Als het kind op de peildatum jonger dan 0 of 4 jaar of ouder is.
    /// </exception>
    public static Leeftijdscategorie Bepaal(DateOnly geboortedatum, DateOnly peildatum)
    {
        if (!ProbeerBepaal(geboortedatum, peildatum, out Leeftijdscategorie categorie))
        {
            int jaren = VolledigeJarenOp(geboortedatum, peildatum);
            throw new ArgumentOutOfRangeException(
                jaren < 0 ? nameof(peildatum) : nameof(geboortedatum),
                jaren < 0
                    ? "De peildatum ligt vóór de geboortedatum; het kind is dan nog niet geboren."
                    : "Het kind is op de peildatum 4 jaar of ouder en valt buiten de leeftijdsschaal 0-4.");
        }

        return categorie;
    }

    /// <summary>
    /// Probeert de leeftijdscategorie te bepalen zonder een exception te werpen.
    /// Geeft <c>false</c> als het kind op de peildatum nog niet geboren is of al
    /// 4 jaar of ouder is (buiten de schaal 0-4). Handig in plannings-loops waar
    /// kinderen op de grens van de opvangleeftijd voorkomen.
    /// </summary>
    public static bool ProbeerBepaal(
        DateOnly geboortedatum, DateOnly peildatum, out Leeftijdscategorie categorie)
    {
        int jaren = VolledigeJarenOp(geboortedatum, peildatum);
        if (jaren is < 0 or > 3)
        {
            categorie = default;
            return false;
        }

        categorie = new Leeftijdscategorie((Leeftijdsgroep)jaren, jaren);
        return true;
    }

    private static int VolledigeJarenOp(DateOnly geboortedatum, DateOnly peildatum)
    {
        int jaren = peildatum.Year - geboortedatum.Year;
        if (geboortedatum.AddYears(jaren) > peildatum)
        {
            jaren--;
        }

        return jaren;
    }
}
