namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// Onveranderlijke definitie van één observatiemoment: de leeftijd (in maanden)
/// waarop het moment valt en of het het bijzondere eindmoment is. De momenten zijn
/// gekoppeld aan de LEEFTIJD van het kind (afgeleid van de geboortedatum), niet aan
/// "laatste observatie + 6 maanden". De vaste catalogus leeft in
/// <see cref="Services.Observatieschema"/>.
/// </summary>
public readonly record struct Observatiemoment
{
    public Observatiemoment(int mijlpaalMaanden, bool isEindmoment)
    {
        if (mijlpaalMaanden <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mijlpaalMaanden), mijlpaalMaanden, "Een mijlpaal moet in hele maanden > 0 zijn.");
        }

        MijlpaalMaanden = mijlpaalMaanden;
        IsEindmoment = isEindmoment;
    }

    /// <summary>De leeftijd in maanden waarop dit observatiemoment valt (bijv. 6, 12, 46).</summary>
    public int MijlpaalMaanden { get; }

    /// <summary>
    /// Of dit het bijzondere eindmoment (3 jaar en 10 maanden) is: de laatste
    /// observatie, ~2 maanden voordat het kind 4 wordt en uit de dagopvang stroomt.
    /// </summary>
    public bool IsEindmoment { get; }

    /// <summary>Leesbare omschrijving in het Nederlands, bijv. "1 jaar en 6 maanden".</summary>
    public string Beschrijving => MaakBeschrijving(MijlpaalMaanden);

    private static string MaakBeschrijving(int maanden)
    {
        int jaren = maanden / 12;
        int rest = maanden % 12;

        if (jaren == 0)
        {
            return $"{rest} maanden";
        }

        string jaarDeel = jaren == 1 ? "1 jaar" : $"{jaren} jaar";
        return rest == 0 ? jaarDeel : $"{jaarDeel} en {rest} maanden";
    }
}
