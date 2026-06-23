using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Portaal;

/// <summary>Eén ingeplande dienst van de dag, zoals het Groepsportaal die op locatie toont.</summary>
public sealed record DagdienstDto(
    Guid DienstId,
    Guid MedewerkerId,
    string MedewerkerNaam,
    Guid StamgroepId,
    string StamgroepNaam,
    string? Taakomschrijving,
    Dienstsoort Dienstsoort);

/// <summary>De diensten van één dag op locatie, met de verstuurd-status van de week.</summary>
public sealed record GroepsportaalDagDto(
    DateOnly Datum,
    bool RoosterVerstuurd,
    IReadOnlyList<DagdienstDto> Diensten);

/// <summary>Minimale medewerkerkeuze voor het tablet (om jezelf te selecteren bij inklokken).</summary>
public sealed record PortaalMedewerkerDto(Guid Id, string Naam);
