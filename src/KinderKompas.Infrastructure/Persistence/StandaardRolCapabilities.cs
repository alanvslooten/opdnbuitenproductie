using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// De verstandige default-rechtenmatrix waarmee een nieuwe organisatie wordt
/// geseed. Dit is alleen een STARTWAARDE: de mapping leeft daarna in data
/// (<c>RolCapability</c>) en is door de Beheerder per rol aan te passen.
///
/// Privacy-kernregel zit hier verankerd: <see cref="Capabilities.MagOudergegevensZien"/>
/// gaat standaard alleen naar <see cref="Rol.Beheerder"/> en
/// <see cref="Rol.Groepsportaal"/> (op locatie) — bewust NIET naar de
/// medewerker-rollen, die in het thuis-portaal werken.
/// </summary>
public static class StandaardRolCapabilities
{
    public static readonly IReadOnlyDictionary<Rol, string[]> Standaard = new Dictionary<Rol, string[]>
    {
        [Rol.Beheerder] = new[]
        {
            Capabilities.MagOudergegevensZien,
            Capabilities.MagKinderenBeheren,
            Capabilities.MagPlanningZien,
            Capabilities.MagWachtlijstBeheren,
            Capabilities.MagRoosterBeheren,
            Capabilities.MagRoosterVersturen,
            Capabilities.MagObservatiesVersturen,
            Capabilities.MagMedewerkersBeheren,
            Capabilities.MagInstellingenBeheren,
            Capabilities.MagDashboardZien,
            Capabilities.MagThuisportaalGebruiken,
        },
        [Rol.Hulpbeheerder] = new[]
        {
            Capabilities.MagKinderenBeheren,
            Capabilities.MagPlanningZien,
            Capabilities.MagWachtlijstBeheren,
            Capabilities.MagRoosterBeheren,
            Capabilities.MagRoosterVersturen,
            Capabilities.MagObservatiesVersturen,
            Capabilities.MagMedewerkersBeheren,
            Capabilities.MagDashboardZien,
            Capabilities.MagThuisportaalGebruiken,
        },
        // Senior werkt op de groep mee: planning inzien + observaties, maar bewust
        // GEEN dashboard, kindbeheer, stamgroepen of wachtlijst (feedback Erik V2).
        [Rol.Senior] = new[]
        {
            Capabilities.MagPlanningZien,
            Capabilities.MagRoosterVersturen,
            Capabilities.MagObservatiesVersturen,
            Capabilities.MagThuisportaalGebruiken,
        },
        [Rol.Junior] = new[]
        {
            Capabilities.MagObservatiesVersturen,
            Capabilities.MagThuisportaalGebruiken,
        },
        // Stagiair: minimale rechten — alleen het eigen thuis-portaal (rooster/uren/
        // beschikbaarheid). Geen observaties/planning tenzij de beheerder dat toekent.
        [Rol.Stagiair] = new[]
        {
            Capabilities.MagThuisportaalGebruiken,
        },
        // Gedeeld tablet-account op locatie: geen MedewerkerId, dus geen thuis-portaal.
        // Tablet op locatie: kinderen ALLEEN-LEZEN (geen MagKinderenBeheren → geen
        // aanmaken/bewerken/verwijderen en geen stamgroepen-beheer, feedback Erik V2).
        // Lezen van kinderen + oudergegevens loopt via het groepsportaal-endpoint.
        [Rol.Groepsportaal] = new[]
        {
            Capabilities.MagOudergegevensZien,
            Capabilities.MagKinderenLezen,
            Capabilities.MagPlanningZien,
            Capabilities.MagObservatiesVersturen,
            Capabilities.MagGroepsportaalGebruiken,
        },
    };
}
