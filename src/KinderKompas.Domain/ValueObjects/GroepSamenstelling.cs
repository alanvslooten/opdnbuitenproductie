using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// Onveranderlijke momentopname van een groep op één peildatum: hoeveel kinderen
/// er per wettelijke leeftijdscategorie aanwezig zijn. Dit is de input voor de
/// <see cref="Services.BkrCalculator"/>. De fysieke stamgroep staat hier los van;
/// het gaat puur om de leeftijdsopbouw die de BKR bepaalt.
/// </summary>
public readonly record struct GroepSamenstelling
{
    /// <summary>Aantal kinderen van 0 tot 1 jaar (de "baby's").</summary>
    public int AantalNulTotEen { get; }

    /// <summary>Aantal kinderen van 1 tot 2 jaar.</summary>
    public int AantalEenTotTwee { get; }

    /// <summary>Aantal kinderen van 2 tot 3 jaar.</summary>
    public int AantalTweeTotDrie { get; }

    /// <summary>Aantal kinderen van 3 tot 4 jaar.</summary>
    public int AantalDrieTotVier { get; }

    public GroepSamenstelling(
        int aantalNulTotEen,
        int aantalEenTotTwee,
        int aantalTweeTotDrie,
        int aantalDrieTotVier)
    {
        if (aantalNulTotEen < 0 || aantalEenTotTwee < 0 ||
            aantalTweeTotDrie < 0 || aantalDrieTotVier < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aantalNulTotEen),
                "Een aantal kinderen kan niet negatief zijn.");
        }

        AantalNulTotEen = aantalNulTotEen;
        AantalEenTotTwee = aantalEenTotTwee;
        AantalTweeTotDrie = aantalTweeTotDrie;
        AantalDrieTotVier = aantalDrieTotVier;
    }

    /// <summary>Totaal aantal kinderen in de groep.</summary>
    public int Totaal =>
        AantalNulTotEen + AantalEenTotTwee + AantalTweeTotDrie + AantalDrieTotVier;

    /// <summary>Of de groep leeg is (geen kinderen).</summary>
    public bool IsLeeg => Totaal == 0;

    /// <summary>Of er kinderen van 0-1 jaar aanwezig zijn (bepaalt of Formule Z verplicht is).</summary>
    public bool BevatBabys => AantalNulTotEen > 0;

    /// <summary>Het aantal kinderen in één specifieke leeftijdsgroep.</summary>
    public int AantalIn(Leeftijdsgroep groep) => groep switch
    {
        Leeftijdsgroep.NulTotEen => AantalNulTotEen,
        Leeftijdsgroep.EenTotTwee => AantalEenTotTwee,
        Leeftijdsgroep.TweeTotDrie => AantalTweeTotDrie,
        Leeftijdsgroep.DrieTotVier => AantalDrieTotVier,
        _ => throw new ArgumentOutOfRangeException(nameof(groep))
    };

    /// <summary>De leeftijdsgroepen die daadwerkelijk in de groep aanwezig zijn, oplopend.</summary>
    public IReadOnlyList<Leeftijdsgroep> AanwezigeGroepen
    {
        get
        {
            var groepen = new List<Leeftijdsgroep>(4);
            if (AantalNulTotEen > 0) groepen.Add(Leeftijdsgroep.NulTotEen);
            if (AantalEenTotTwee > 0) groepen.Add(Leeftijdsgroep.EenTotTwee);
            if (AantalTweeTotDrie > 0) groepen.Add(Leeftijdsgroep.TweeTotDrie);
            if (AantalDrieTotVier > 0) groepen.Add(Leeftijdsgroep.DrieTotVier);
            return groepen;
        }
    }

    /// <summary>Of de groep precies één leeftijdscategorie bevat (stamgroep één leeftijd).</summary>
    public bool IsEnkeleLeeftijd => AanwezigeGroepen.Count == 1;

    /// <summary>Of de groep meerdere leeftijdscategorieën bevat (gemengde groep).</summary>
    public bool IsGemengd => AanwezigeGroepen.Count > 1;

    /// <summary>
    /// Een nieuwe samenstelling met één extra kind in de gegeven leeftijdsgroep.
    /// Gebruikt om de BKR-impact van een plaatsing te bepalen: de samenstelling
    /// mét de kandidaat erbij gaat door dezelfde <see cref="Services.BkrCalculator"/>,
    /// zodat de "wat-als"-uitkomst gegarandeerd uit de wettelijke rekenkern komt.
    /// </summary>
    public GroepSamenstelling MetExtra(Leeftijdsgroep groep) => groep switch
    {
        Leeftijdsgroep.NulTotEen => new(AantalNulTotEen + 1, AantalEenTotTwee, AantalTweeTotDrie, AantalDrieTotVier),
        Leeftijdsgroep.EenTotTwee => new(AantalNulTotEen, AantalEenTotTwee + 1, AantalTweeTotDrie, AantalDrieTotVier),
        Leeftijdsgroep.TweeTotDrie => new(AantalNulTotEen, AantalEenTotTwee, AantalTweeTotDrie + 1, AantalDrieTotVier),
        Leeftijdsgroep.DrieTotVier => new(AantalNulTotEen, AantalEenTotTwee, AantalTweeTotDrie, AantalDrieTotVier + 1),
        _ => throw new ArgumentOutOfRangeException(nameof(groep))
    };

    /// <summary>
    /// Bouwt een samenstelling uit de geboortedata van de aanwezige kinderen op een
    /// peildatum. De leeftijdscategorie wordt per kind afgeleid via
    /// <see cref="Leeftijdscategorie.Bepaal"/> (grens = de verjaardag).
    /// </summary>
    public static GroepSamenstelling VanafGeboortedata(
        IEnumerable<DateOnly> geboortedata, DateOnly peildatum)
    {
        ArgumentNullException.ThrowIfNull(geboortedata);

        int nul = 0, een = 0, twee = 0, drie = 0;
        foreach (var geboortedatum in geboortedata)
        {
            switch (Leeftijdscategorie.Bepaal(geboortedatum, peildatum).Groep)
            {
                case Leeftijdsgroep.NulTotEen: nul++; break;
                case Leeftijdsgroep.EenTotTwee: een++; break;
                case Leeftijdsgroep.TweeTotDrie: twee++; break;
                case Leeftijdsgroep.DrieTotVier: drie++; break;
            }
        }

        return new GroepSamenstelling(nul, een, twee, drie);
    }
}
