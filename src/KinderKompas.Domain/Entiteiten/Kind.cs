using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een kind dat opvang afneemt. De leeftijdscategorie wordt NIET opgeslagen
/// maar afgeleid uit <see cref="Geboortedatum"/> op een peildatum.
/// </summary>
public class Kind : TenantEntiteit
{
    public required string Voornaam { get; set; }
    public required string Achternaam { get; set; }
    public DateOnly Geboortedatum { get; set; }

    /// <summary>
    /// Privacy-gevoelige contactgegevens van de ouders/verzorgers/voogden. Een kind
    /// kan er meerdere hebben (minimaal twee is wenselijk); de lijst kan leeg zijn als
    /// er nog niets is ingevuld. Zichtbaarheid loopt via de capability
    /// <c>MagOudergegevensZien</c> en DTO-projectie. De eerste in de lijst geldt als
    /// het primaire contact (o.a. voor het mailen van observaties).
    /// </summary>
    public List<Oudercontact> Oudercontacten { get; set; } = new();

    /// <summary>
    /// De vaste <b>thuisgroep</b> van het kind: het pedagogische anker voor
    /// oudergegevens, mentor en observaties, en de default voor de dagindeling.
    /// Afwijkingen per dag (ruildag, incidenteel op een andere groep, afwezig) lopen
    /// via <see cref="Dagplaatsing"/>; zie <see cref="Services.Dagindeling"/>.
    /// </summary>
    public Guid StamgroepId { get; set; }
    public Stamgroep? Stamgroep { get; set; }

    /// <summary>Het contact (ouder/verzorger/gezin) waartoe dit kind hoort. Optioneel.</summary>
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }

    /// <summary>
    /// De mentor-medewerker (pedagogisch medewerker) van dit kind. In de Nederlandse
    /// kinderopvang is de mentor wettelijk verplicht en verantwoordelijk voor de
    /// observaties en oudergesprekken. Optioneel. Stuurt de zichtbaarheid van
    /// observaties: een medewerker ziet alleen de kinderen waarvan hij mentor is
    /// (een leidinggevende ziet alles). De relatie loopt via de bestaande
    /// <c>MentorId</c>-kolom; EF-conventie linkt <see cref="Mentor"/> ↔ MentorId.
    /// </summary>
    public Guid? MentorId { get; set; }
    public Medewerker? Mentor { get; set; }

    /// <summary>Eerste opvangdag. Verplicht.</summary>
    public DateOnly Startdatum { get; set; }

    /// <summary>
    /// Laatste opvangdag. Optioneel: is deze niet ingevuld, dan geldt wettelijk
    /// de vierde verjaardag als einddatum (zie <see cref="EffectieveEinddatum"/>).
    /// </summary>
    public DateOnly? Einddatum { get; set; }

    public Contracttype Contracttype { get; set; }

    /// <summary>Gewenste opvangdagen (input voor planning, niet de officiële aan-/afmelding).</summary>
    public Weekdag GewensteOpvangdagen { get; set; }

    public Organisatie? Organisatie { get; set; }

    /// <summary>
    /// De effectieve einddatum: de expliciet ingevulde einddatum, of anders de
    /// vierde verjaardag van het kind (daarna stroomt het door naar de BSO/school).
    /// </summary>
    public DateOnly EffectieveEinddatum => Einddatum ?? Geboortedatum.AddYears(4);

    /// <summary>De datum waarop het kind 4 jaar wordt en uit de dagopvang stroomt.</summary>
    public DateOnly VierdeVerjaardag => Geboortedatum.AddYears(4);

    /// <summary>Leeftijdscategorie van het kind op de gegeven peildatum.</summary>
    public Leeftijdscategorie LeeftijdscategorieOp(DateOnly peildatum)
        => Leeftijdscategorie.Bepaal(Geboortedatum, peildatum);

    /// <summary>
    /// Of het kind binnen de gegeven marge (standaard 90 dagen) 4 jaar wordt en dus
    /// bijna uit de dagopvang stroomt — een signaal voor de planner om de uitstroom
    /// en eventuele BSO-overgang voor te bereiden. Op of na de verjaardag zelf is
    /// het signaal niet meer "bijna".
    /// </summary>
    public bool WordtBinnenkortVier(DateOnly peildatum, int margeInDagen = 90)
    {
        DateOnly vierde = VierdeVerjaardag;
        return peildatum < vierde && vierde <= peildatum.AddDays(margeInDagen);
    }
}
