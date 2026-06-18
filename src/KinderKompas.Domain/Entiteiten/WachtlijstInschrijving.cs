using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een inschrijving op de wachtlijst: een (nog) niet-geplaatst kind met de
/// aanvraag van de ouder (gewenste dagen, startdatum, contracttype). Dit staat
/// los van <see cref="Kind"/>: een wachtlijstkind is nog niet in een stamgroep
/// geplaatst. De officiële plaatsing en het contract gebeuren in Portabase
/// (buiten scope); KinderKompas bewaakt enkel de wachtlijst en de voorstellen.
///
/// <para>De leeftijdscategorie wordt — net als bij <see cref="Kind"/> — NIET
/// opgeslagen maar afgeleid uit <see cref="Geboortedatum"/> op een peildatum.</para>
/// </summary>
public class WachtlijstInschrijving : TenantEntiteit
{
    public required string Voornaam { get; set; }
    public required string Achternaam { get; set; }
    public DateOnly Geboortedatum { get; set; }

    /// <summary>
    /// Contactgegevens van de ouder/verzorger. Privacy-gevoelig: zichtbaarheid
    /// loopt — net als bij <see cref="Kind"/> — via de capability
    /// <c>MagOudergegevensZien</c> en DTO-projectie.
    /// </summary>
    public Oudercontact? Oudercontact { get; set; }

    /// <summary>De datum waarop het kind op de wachtlijst is gezet (bepaalt de anciënniteit).</summary>
    public DateOnly InschrijfdatumWachtlijst { get; set; }

    /// <summary>Gewenste eerste opvangdag volgens de ouder.</summary>
    public DateOnly GewensteStartdatum { get; set; }

    /// <summary>De gewenste opvangdagen (bit-vlaggen ma t/m vr).</summary>
    public Weekdag GewensteOpvangdagen { get; set; }

    public Contracttype Contracttype { get; set; }

    /// <summary>
    /// De stamgroep waar de ouder/organisatie naar toe wil plaatsen. Optioneel:
    /// is er (nog) geen voorkeur, dan kiest de planner de groep pas bij het
    /// opstellen van een voorstel.
    /// </summary>
    public Guid? GewensteStamgroepId { get; set; }
    public Stamgroep? GewensteStamgroep { get; set; }

    /// <summary>
    /// Of dit een interne inschrijving is (broertje/zusje van een al geplaatst
    /// kind of een doorstroom binnen de organisatie). Interne aanvragen krijgen
    /// voorrang in de prioriteitsscore (zie <see cref="Services.WachtlijstPrioriteit"/>).
    /// </summary>
    public bool IsIntern { get; set; }

    /// <summary>
    /// Handmatig bovenaan gezet (typisch een personeelskind). Overstijgt de
    /// prioriteitsscore: deze kinderen staan altijd vóór in de sortering.
    /// </summary>
    public bool HandmatigBovenaan { get; set; }

    /// <summary>
    /// De dagen die al via een geaccepteerd (deel)voorstel zijn geplaatst. Bij een
    /// deelvoorstel worden hier alleen de voorgestelde dagen aan toegevoegd, zodat
    /// de resterende gewenste dagen op de wachtlijst blijven staan.
    /// </summary>
    public Weekdag ReedsGeplaatsteDagen { get; set; } = Weekdag.Geen;

    public WachtlijstStatus Status { get; set; } = WachtlijstStatus.Wachtend;

    /// <summary>Vrije notitie van de planner (bijv. context bij een interne aanvraag).</summary>
    public string? Notitie { get; set; }

    public Organisatie? Organisatie { get; set; }

    /// <summary>De voorstelhistorie van deze inschrijving (alle verstuurde voorstellen).</summary>
    public ICollection<Voorstel> Voorstellen { get; set; } = new List<Voorstel>();

    /// <summary>
    /// De gewenste dagen die nog NIET via een geaccepteerd voorstel zijn geplaatst
    /// — wat er feitelijk nog op de wachtlijst staat.
    /// </summary>
    public Weekdag OpenstaandeDagen => GewensteOpvangdagen & ~ReedsGeplaatsteDagen;

    /// <summary>Of alle gewenste dagen inmiddels geplaatst zijn.</summary>
    public bool IsVolledigGeplaatst => OpenstaandeDagen == Weekdag.Geen;

    /// <summary>De vierde verjaardag: na deze datum stroomt het kind uit de dagopvang.</summary>
    public DateOnly VierdeVerjaardag => Geboortedatum.AddYears(4);

    /// <summary>
    /// Verwerkt een geaccepteerd (deel)voorstel: de voorgestelde dagen die ook
    /// daadwerkelijk gewenst zijn, worden als geplaatst gemarkeerd. Staan er
    /// daarna geen gewenste dagen meer open, dan is de inschrijving volledig
    /// geplaatst. De resterende, niet-voorgestelde dagen blijven wachtend.
    /// </summary>
    public void VerwerkGeaccepteerdVoorstel(Weekdag voorgesteldeDagen)
    {
        ReedsGeplaatsteDagen |= voorgesteldeDagen & GewensteOpvangdagen;
        if (IsVolledigGeplaatst && Status == WachtlijstStatus.Wachtend)
        {
            Status = WachtlijstStatus.Geplaatst;
        }
    }
}
