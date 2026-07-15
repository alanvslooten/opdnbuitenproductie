using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Services;

/// <summary>
/// Bepaalt — puur en deterministisch — de EFFECTIEVE groepsindeling per dag: welke
/// kinderen op een gegeven datum op een gegeven groep staan, rekening houdend met
/// dagafwijkingen (<see cref="Dagplaatsing"/>) bovenop het reguliere opvangpatroon.
///
/// De rekenregel per kind op een datum D:
///   1. Bestaat er een <see cref="Dagplaatsing"/> voor (kind, D)? Dan beslist die
///      volledig: aanwezig op <see cref="Dagplaatsing.StamgroepId"/>, of afwezig als
///      die null is. Het reguliere patroon wordt voor D genegeerd.
///   2. Anders beslist het reguliere patroon (<see cref="Aanwezigheid.IsKindAanwezigOp"/>)
///      met de thuisgroep (<see cref="Kind.StamgroepId"/>) als groep.
///
/// Zonder enige dagafwijking valt deze service exact samen met <see cref="Aanwezigheid"/>.
/// Geen database- of UI-afhankelijkheid; volledig unit-testbaar.
/// Zie docs/dagplaatsing-ontwerp.md.
/// </summary>
public static class Dagindeling
{
    /// <summary>
    /// De groep waarop het kind op <paramref name="datum"/> effectief staat, of
    /// <c>null</c> als het kind die dag niet aanwezig is.
    /// </summary>
    /// <param name="kind">Het kind.</param>
    /// <param name="datum">De peildatum.</param>
    /// <param name="afwijking">
    /// De dagafwijking voor (kind, datum) als die bestaat, anders <c>null</c>.
    /// </param>
    /// <param name="vakanties">Schoolvakanties (voor het reguliere patroon bij 40-wekencontract).</param>
    public static Guid? EffectieveGroepOp(
        Kind kind,
        DateOnly datum,
        Dagplaatsing? afwijking,
        IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(vakanties);

        // Een afwijking beslist volledig voor deze datum (ook afwezigheid: StamgroepId == null).
        if (afwijking is not null)
        {
            return afwijking.StamgroepId;
        }

        // Geen afwijking: het reguliere patroon met de thuisgroep als groep.
        return Aanwezigheid.IsKindAanwezigOp(kind, datum, vakanties)
            ? kind.StamgroepId
            : (Guid?)null;
    }

    /// <summary>
    /// De kinderen die op <paramref name="datum"/> effectief op groep
    /// <paramref name="groepId"/> staan. Kijkt over ALLE meegegeven kinderen (niet
    /// vooraf op thuisgroep gefilterd), omdat een afwijking een kind ook op een andere
    /// dan zijn thuisgroep kan zetten.
    /// </summary>
    /// <param name="kinderen">Alle kinderen die kandidaat kunnen zijn (bijv. alle actieve kinderen).</param>
    /// <param name="groepId">De groep waarvan we de bezetting willen.</param>
    /// <param name="datum">De peildatum.</param>
    /// <param name="afwijkingen">Alle dagafwijkingen (worden geïndexeerd op (kind, datum)).</param>
    /// <param name="vakanties">Schoolvakanties.</param>
    public static IReadOnlyList<Kind> OpGroepOpDag(
        IEnumerable<Kind> kinderen,
        Guid groepId,
        DateOnly datum,
        IEnumerable<Dagplaatsing> afwijkingen,
        IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(kinderen);
        ArgumentNullException.ThrowIfNull(afwijkingen);
        ArgumentNullException.ThrowIfNull(vakanties);

        IReadOnlyDictionary<Guid, Dagplaatsing> perKind = IndexeerOpKind(afwijkingen, datum);
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();

        var resultaat = new List<Kind>();
        foreach (Kind kind in kinderen)
        {
            perKind.TryGetValue(kind.Id, out Dagplaatsing? afwijking);
            if (EffectieveGroepOp(kind, datum, afwijking, vakantieLijst) == groepId)
            {
                resultaat.Add(kind);
            }
        }

        return resultaat;
    }

    /// <summary>
    /// De kinderen die op <paramref name="datum"/> effectief ergens aanwezig zijn
    /// (op welke groep dan ook), rekening houdend met dagafwijkingen. Voor het dagfilter
    /// "wie is er vandaag?" zonder specifieke groep.
    /// </summary>
    public static IReadOnlyList<Kind> AanwezigOp(
        IEnumerable<Kind> kinderen,
        DateOnly datum,
        IEnumerable<Dagplaatsing> afwijkingen,
        IEnumerable<Schoolvakantie> vakanties)
    {
        ArgumentNullException.ThrowIfNull(kinderen);
        ArgumentNullException.ThrowIfNull(afwijkingen);
        ArgumentNullException.ThrowIfNull(vakanties);

        IReadOnlyDictionary<Guid, Dagplaatsing> perKind = IndexeerOpKind(afwijkingen, datum);
        IReadOnlyList<Schoolvakantie> vakantieLijst =
            vakanties as IReadOnlyList<Schoolvakantie> ?? vakanties.ToList();

        var resultaat = new List<Kind>();
        foreach (Kind kind in kinderen)
        {
            perKind.TryGetValue(kind.Id, out Dagplaatsing? afwijking);
            if (EffectieveGroepOp(kind, datum, afwijking, vakantieLijst) is not null)
            {
                resultaat.Add(kind);
            }
        }

        return resultaat;
    }

    /// <summary>
    /// De leeftijdsopbouw (<see cref="GroepSamenstelling"/>) van de kinderen die op
    /// <paramref name="datum"/> effectief op groep <paramref name="groepId"/> staan —
    /// directe input voor de <see cref="BkrCalculator"/>, nu inclusief dagafwijkingen.
    /// </summary>
    public static GroepSamenstelling SamenstellingOpGroepOpDag(
        IEnumerable<Kind> kinderen,
        Guid groepId,
        DateOnly datum,
        IEnumerable<Dagplaatsing> afwijkingen,
        IEnumerable<Schoolvakantie> vakanties)
    {
        IReadOnlyList<Kind> aanwezig = OpGroepOpDag(kinderen, groepId, datum, afwijkingen, vakanties);
        return GroepSamenstelling.VanafGeboortedata(aanwezig.Select(k => k.Geboortedatum), datum);
    }

    /// <summary>
    /// Indexeert de afwijkingen die op <paramref name="datum"/> gelden op KindId. Er is
    /// per (kind, datum) hooguit één afwijking (unieke sleutel in de database); mocht er
    /// door dubbele data toch meer zijn, dan wint de laatste.
    /// </summary>
    private static IReadOnlyDictionary<Guid, Dagplaatsing> IndexeerOpKind(
        IEnumerable<Dagplaatsing> afwijkingen, DateOnly datum)
    {
        var perKind = new Dictionary<Guid, Dagplaatsing>();
        foreach (Dagplaatsing afwijking in afwijkingen)
        {
            if (afwijking.Datum == datum)
            {
                perKind[afwijking.KindId] = afwijking;
            }
        }

        return perKind;
    }
}
