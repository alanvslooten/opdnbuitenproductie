namespace KinderKompas.Domain.Autorisatie;

/// <summary>
/// De vaste set capabilities (fijnmazige rechten) die het systeem kent. Dit is
/// het CONTRACT: de sleutels zijn constanten zodat policies en projecties er
/// type-veilig naar verwijzen i.p.v. naar magische strings.
///
/// LET OP het onderscheid:
/// - WELKE capabilities bestaan = systeemkennis → staat hier vast.
/// - WELKE rol welke capability krijgt = configuratie → leeft in data
///   (<see cref="Entiteiten.RolCapability"/>), zodat de Beheerder dit later
///   per rol kan aanpassen (fase 9) zonder code-wijziging.
/// </summary>
public static class Capabilities
{
    /// <summary>
    /// Oudergegevens (naam, telefoon, mail) inzien. Privacy-kernregel: standaard
    /// alleen op de Groepsportaal (op locatie) en voor de Beheerder; bewust NIET
    /// in het thuis-portaal van een medewerker.
    /// </summary>
    public const string MagOudergegevensZien = "MagOudergegevensZien";

    public const string MagKinderenBeheren = "MagKinderenBeheren";

    /// <summary>
    /// Kindgegevens ALLEEN-LEZEN inzien (lijst + detail + oudergegevens), zonder te kunnen
    /// muteren. Voor het Groepsportaal: de tablet ziet de kinderen wél maar bewerkt ze niet.
    /// </summary>
    public const string MagKinderenLezen = "MagKinderenLezen";

    /// <summary>
    /// De weekplanning en het dagfilter ALLEEN-LEZEN inzien, los van
    /// <see cref="MagKinderenBeheren"/> (dat ook kindgegevens en stamgroepen muteert).
    /// Zo kan een rol als Senior wél de planning bekijken zonder kinderen/stamgroepen
    /// te kunnen beheren.
    /// </summary>
    public const string MagPlanningZien = "MagPlanningZien";

    public const string MagWachtlijstBeheren = "MagWachtlijstBeheren";
    public const string MagRoosterBeheren = "MagRoosterBeheren";
    public const string MagRoosterVersturen = "MagRoosterVersturen";
    public const string MagObservatiesVersturen = "MagObservatiesVersturen";
    public const string MagMedewerkersBeheren = "MagMedewerkersBeheren";
    public const string MagInstellingenBeheren = "MagInstellingenBeheren";

    /// <summary>
    /// Toegang tot het dashboard én het app-brede actiecentrum (meldingen/to-do's)
    /// uit fase 9. Voor de back-office-rollen die de organisatie aansturen; bewust
    /// NIET voor het gedeelde tablet-account of een puur thuis-werkende medewerker.
    /// </summary>
    public const string MagDashboardZien = "MagDashboardZien";

    /// <summary>
    /// Toegang tot het Groepsportaal (fase 8): de gedeelde tablet-context op locatie.
    /// In-/uitklokken, de dienst van de dag inzien en — samen met
    /// <see cref="MagOudergegevensZien"/> — oudergegevens en observaties van de
    /// kinderen op locatie. Standaard alleen voor <see cref="Enums.Rol.Groepsportaal"/>.
    /// </summary>
    public const string MagGroepsportaalGebruiken = "MagGroepsportaalGebruiken";

    /// <summary>
    /// Toegang tot het Thuis-portaal (fase 8): de persoonlijke medewerker-context.
    /// Eigen (verstuurd) rooster, beschikbaarheid opgeven, verlof aanvragen en eigen
    /// saldo/uren. Bewust ZONDER oudergegevens. Voor de medewerker-rollen die met een
    /// persoonlijk account (en gekoppelde medewerker) werken.
    /// </summary>
    public const string MagThuisportaalGebruiken = "MagThuisportaalGebruiken";

    /// <summary>
    /// Alle bekende capabilities met omschrijving. Bron voor het seeden van de
    /// <see cref="Entiteiten.Capability"/>-referentietabel.
    /// </summary>
    public static readonly IReadOnlyList<CapabilityDefinitie> Alle = new[]
    {
        new CapabilityDefinitie(MagOudergegevensZien, "Oudergegevens (contact, telefoon, mail) inzien"),
        new CapabilityDefinitie(MagKinderenBeheren, "Kindgegevens en plaatsing beheren"),
        new CapabilityDefinitie(MagKinderenLezen, "Kindgegevens inzien (alleen-lezen)"),
        new CapabilityDefinitie(MagPlanningZien, "Weekplanning en dagfilter inzien (alleen-lezen)"),
        new CapabilityDefinitie(MagWachtlijstBeheren, "Wachtlijst en plaatsingsvoorstellen beheren"),
        new CapabilityDefinitie(MagRoosterBeheren, "Werkrooster opstellen en wijzigen"),
        new CapabilityDefinitie(MagRoosterVersturen, "Werkrooster definitief versturen"),
        new CapabilityDefinitie(MagObservatiesVersturen, "Observaties opstellen en versturen"),
        new CapabilityDefinitie(MagMedewerkersBeheren, "Medewerkers en hun rollen beheren"),
        new CapabilityDefinitie(MagInstellingenBeheren, "Organisatie-instellingen en rechten beheren"),
        new CapabilityDefinitie(MagDashboardZien, "Dashboard en het actiecentrum (meldingen/to-do's) inzien"),
        new CapabilityDefinitie(MagGroepsportaalGebruiken, "Groepsportaal op locatie gebruiken (inklokken, dienst, observaties)"),
        new CapabilityDefinitie(MagThuisportaalGebruiken, "Thuis-portaal gebruiken (eigen rooster, beschikbaarheid, verlof)"),
    };
}

/// <summary>Definitie van één capability: de sleutel en een leesbare omschrijving.</summary>
public sealed record CapabilityDefinitie(string Sleutel, string Omschrijving);
