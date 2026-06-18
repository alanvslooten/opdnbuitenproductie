using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een schoolvakantieperiode binnen een schooljaar. Bepaalt waar kinderen met een
/// 40-wekencontract (alleen schoolweken) NIET worden ingepland: in een
/// schoolvakantieweek nemen zij geen opvang af. Kinderen met een 49-wekencontract
/// lopen door. De regel zelf leeft in <see cref="Services.Aanwezigheid"/>; deze
/// entiteit is enkel het ingevoerde datumbereik.
/// </summary>
public class Schoolvakantie : TenantEntiteit
{
    /// <summary>Naam van de vakantie, bijv. "Zomervakantie" of "Voorjaarsvakantie".</summary>
    public required string Naam { get; set; }

    /// <summary>
    /// Het beginjaar van het schooljaar (een schooljaar loopt augustus t/m juli).
    /// Bijv. 2025 staat voor schooljaar 2025/2026. Maakt het mogelijk vakanties
    /// per schooljaar te groeperen en in te voeren.
    /// </summary>
    public int Schooljaar { get; set; }

    /// <summary>Eerste vakantiedag (inclusief).</summary>
    public DateOnly Begindatum { get; set; }

    /// <summary>Laatste vakantiedag (inclusief).</summary>
    public DateOnly Einddatum { get; set; }

    public Organisatie? Organisatie { get; set; }

    /// <summary>Of de gegeven datum binnen deze vakantieperiode valt (grenzen inclusief).</summary>
    public bool Omvat(DateOnly datum) => datum >= Begindatum && datum <= Einddatum;

    /// <summary>Leesbaar label voor het schooljaar, bijv. "2025/2026".</summary>
    public string SchooljaarLabel => $"{Schooljaar}/{Schooljaar + 1}";
}
