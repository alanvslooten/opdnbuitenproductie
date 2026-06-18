namespace KinderKompas.Domain.Common;

/// <summary>
/// Markeert een entiteit als tenant-gebonden: ze hoort bij precies één
/// organisatie. De globale EF-queryfilter en de SaveChanges-override gebruiken
/// deze interface om OrganisatieId af te dwingen. De tenant-anker-entiteit
/// (Organisatie zelf) implementeert deze interface bewust NIET.
/// </summary>
public interface ITenantEntiteit
{
    Guid OrganisatieId { get; set; }
}
