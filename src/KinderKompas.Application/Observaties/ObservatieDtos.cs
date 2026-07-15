using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Application.Observaties;

/// <summary>Leesmodel van één afgeronde observatie (de geüploade PDF + verzendstatus).</summary>
public sealed record ObservatieDto(
    Guid Id,
    Guid KindId,
    int MijlpaalMaanden,
    string BestandsNaam,
    long BestandsGrootte,
    DateTime GeuploadOp,
    DateTime? VerzondenOp,
    string? VerzondenNaarEmail);

/// <summary>
/// Eén observatiemoment in het overzicht: de mijlpaal met status, en — als het moment
/// is afgevinkt — de bijbehorende <see cref="ObservatieDto"/>.
/// </summary>
public sealed record ObservatiemomentDto(
    int MijlpaalMaanden,
    bool IsEindmoment,
    string Beschrijving,
    DateOnly Vervaldatum,
    ObservatieStatus Status,
    ObservatieDto? Observatie);

/// <summary>Het volledige observatieschema van één kind op een peildatum, met telling per status.</summary>
public sealed record KindObservatieschemaDto(
    Guid KindId,
    string Voornaam,
    string Achternaam,
    Guid StamgroepId,
    DateOnly Geboortedatum,
    DateOnly VierdeVerjaardag,
    bool WordtBinnenkortVier,
    Guid? MentorId,
    int AantalOverschreden,
    int AantalBinnenkort,
    int AantalAfgerond,
    IReadOnlyList<ObservatiemomentDto> Momenten,
    // Of de huidige gebruiker dit kind mag bewerken (afvinken/versturen/ongedaan).
    // Een groepsportaal-account ziet alle groepen maar bewerkt alleen de eigen groep.
    bool Bewerkbaar = true);

/// <summary>Invoermodel voor het afvinken (uploaden) van een observatie.</summary>
/// <remarks>De PDF zelf komt als aparte multipart-bestandsstroom, niet in dit model.</remarks>
public sealed record ObservatieAfvinkenInvoer(int MijlpaalMaanden);

/// <summary>
/// Bouwt — puur en testbaar — het observatieschema-overzicht van een kind op een
/// peildatum, door de domein-berekening (<see cref="Observatieschema"/>) te combineren
/// met de reeds afgevinkte observaties. Geen database- of UI-afhankelijkheid.
/// </summary>
public static class ObservatieOverzichtBouwer
{
    public static KindObservatieschemaDto Bouw(
        Kind kind,
        IEnumerable<Observatie> observaties,
        DateOnly peildatum,
        int binnenkortDrempelDagen = Observatieschema.StandaardBinnenkortDrempelDagen)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(observaties);

        Dictionary<int, Observatie> perMijlpaal =
            observaties.ToDictionary(o => o.MijlpaalMaanden);
        IReadOnlySet<int> afgerondeMijlpalen = perMijlpaal.Keys.ToHashSet();

        IReadOnlyList<ObservatiemomentStatus> statussen =
            Observatieschema.Bereken(
                kind.Geboortedatum, peildatum, afgerondeMijlpalen, binnenkortDrempelDagen, kind.Startdatum);

        List<ObservatiemomentDto> momenten = statussen.Select(s =>
        {
            perMijlpaal.TryGetValue(s.Moment.MijlpaalMaanden, out Observatie? obs);
            return new ObservatiemomentDto(
                s.Moment.MijlpaalMaanden,
                s.Moment.IsEindmoment,
                s.Moment.Beschrijving,
                s.Vervaldatum,
                s.Status,
                obs is null ? null : NaarDto(obs));
        }).ToList();

        return new KindObservatieschemaDto(
            kind.Id,
            kind.Voornaam,
            kind.Achternaam,
            kind.StamgroepId,
            kind.Geboortedatum,
            kind.VierdeVerjaardag,
            kind.WordtBinnenkortVier(peildatum),
            kind.MentorId,
            momenten.Count(m => m.Status == ObservatieStatus.Overschreden),
            momenten.Count(m => m.Status == ObservatieStatus.Binnenkort),
            momenten.Count(m => m.Status == ObservatieStatus.Afgerond),
            momenten);
    }

    public static ObservatieDto NaarDto(Observatie o) =>
        new(o.Id, o.KindId, o.MijlpaalMaanden, o.BestandsNaam, o.BestandsGrootte,
            o.AangemaaktOp, o.VerzondenOp, o.VerzondenNaarEmail);
}
