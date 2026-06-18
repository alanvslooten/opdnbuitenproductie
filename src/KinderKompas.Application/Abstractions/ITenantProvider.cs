namespace KinderKompas.Application.Abstractions;

/// <summary>
/// Levert de organisatie (tenant) waarbinnen de huidige operatie plaatsvindt.
/// Nu geeft de implementatie een vaste seed-organisatie terug; in fase 3 komt
/// dit uit de JWT-claim van de ingelogde gebruiker. Business-logica vraagt de
/// tenant ALTIJD via deze provider op en hardcodet nooit een OrganisatieId.
/// </summary>
public interface ITenantProvider
{
    Guid CurrentOrganisatieId { get; }
}
