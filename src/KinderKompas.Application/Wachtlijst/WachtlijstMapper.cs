using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Services;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>
/// De instelbare opties (fase 9c) die de wachtlijst-weergave sturen: de
/// prioriteitsgewichten en de 'binnenkort 4'-drempel. <see cref="Standaard"/> gebruikt
/// de code-defaults, zodat aanroepers zonder instellingen ongewijzigd blijven werken.
/// </summary>
public sealed record WachtlijstWeergaveContext(
    WachtlijstPrioriteitsgewichten Gewichten,
    int BinnenkortVierDrempelDagen)
{
    public static WachtlijstWeergaveContext Standaard =>
        new(WachtlijstPrioriteitsgewichten.Standaard, OrganisatieInstellingen.StandaardKindBinnenkortVierDagen);
}

/// <summary>
/// Projecteert een <see cref="WachtlijstInschrijving"/> naar een DTO en berekent
/// daarbij de prioriteitsscore via de Domain-rekenkern. Dwingt — net als
/// <see cref="KindMapper"/> — de privacy-scheiding van oudergegevens af op basis
/// van de capability <see cref="Capabilities.MagOudergegevensZien"/>.
/// </summary>
public static class WachtlijstMapper
{
    public static WachtlijstInschrijvingDto NaarDto(
        WachtlijstInschrijving inschrijving, ICurrentUser gebruiker, DateOnly peildatum,
        WachtlijstWeergaveContext? context = null)
    {
        WachtlijstWeergaveContext opties = context ?? WachtlijstWeergaveContext.Standaard;

        WachtlijstPrioriteitsuitkomst prioriteit =
            WachtlijstPrioriteit.Bereken(inschrijving, peildatum, opties.Gewichten);

        OudercontactDto? oudercontact =
            gebruiker.Heeft(Capabilities.MagOudergegevensZien) && inschrijving.Oudercontact is not null
                ? new OudercontactDto(
                    inschrijving.Oudercontact.Naam,
                    inschrijving.Oudercontact.Telefoon,
                    inschrijving.Oudercontact.Email)
                : null;

        DateOnly vierde = inschrijving.VierdeVerjaardag;
        bool wordtBinnenkortVier =
            peildatum < vierde && vierde <= peildatum.AddDays(opties.BinnenkortVierDrempelDagen);

        // "Voorstel verstuurd": er staat een verstuurd, nog niet beantwoord voorstel open.
        // (Voorstellen moeten daarvoor wel geladen zijn; anders levert dit false.)
        bool heeftOpenVoorstel =
            inschrijving.Voorstellen.Any(v => v.Status == Domain.Enums.VoorstelStatus.Verstuurd);

        return new WachtlijstInschrijvingDto(
            inschrijving.Id,
            inschrijving.Voornaam,
            inschrijving.Achternaam,
            inschrijving.Geboortedatum,
            inschrijving.InschrijfdatumWachtlijst,
            inschrijving.GewensteStartdatum,
            inschrijving.GewensteOpvangdagen,
            inschrijving.OpenstaandeDagen,
            inschrijving.ReedsGeplaatsteDagen,
            inschrijving.Contracttype,
            inschrijving.GewensteStamgroepId,
            inschrijving.IsIntern,
            inschrijving.HandmatigBovenaan,
            inschrijving.Status,
            heeftOpenVoorstel,
            inschrijving.Notitie,
            prioriteit.Score,
            prioriteit.Onderdelen,
            wordtBinnenkortVier,
            oudercontact);
    }

    /// <summary>Zet de waarden uit een invoermodel op een (nieuwe of bestaande) inschrijving.</summary>
    public static void PasInvoerToe(WachtlijstInschrijving inschrijving, WachtlijstInvoer invoer)
    {
        inschrijving.Voornaam = invoer.Voornaam;
        inschrijving.Achternaam = invoer.Achternaam;
        inschrijving.Geboortedatum = invoer.Geboortedatum;
        inschrijving.InschrijfdatumWachtlijst = invoer.InschrijfdatumWachtlijst;
        inschrijving.GewensteStartdatum = invoer.GewensteStartdatum;
        inschrijving.GewensteOpvangdagen = invoer.GewensteOpvangdagen;
        inschrijving.Contracttype = invoer.Contracttype;
        inschrijving.GewensteStamgroepId = invoer.GewensteStamgroepId;
        inschrijving.IsIntern = invoer.IsIntern;
        inschrijving.HandmatigBovenaan = invoer.HandmatigBovenaan;
        inschrijving.Notitie = invoer.Notitie;
        inschrijving.Oudercontact = invoer.Oudercontact is null
            ? null
            : new Oudercontact(
                invoer.Oudercontact.Naam,
                invoer.Oudercontact.Telefoon,
                invoer.Oudercontact.Email);
    }
}

/// <summary>
/// Sortering van de wachtlijst: handmatig bovenaan gezette kinderen (personeels-
/// kinderen) eerst, dan op aflopende prioriteitsscore, en bij gelijke score de
/// langst wachtende (oudste inschrijfdatum) eerst.
/// </summary>
public static class WachtlijstSortering
{
    public static IReadOnlyList<WachtlijstInschrijvingDto> OpPrioriteit(
        IEnumerable<WachtlijstInschrijvingDto> inschrijvingen)
        => inschrijvingen
            .OrderByDescending(i => i.HandmatigBovenaan)
            .ThenByDescending(i => i.Prioriteitsscore)
            .ThenBy(i => i.InschrijfdatumWachtlijst)
            .ThenBy(i => i.Achternaam)
            .ToList();
}
