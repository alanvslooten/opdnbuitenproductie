using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Het tenant-anker. Elke bedrijfs-entiteit verwijst via OrganisatieId hiernaar.
/// Nu bedienen we één organisatie ("Op d'n Buiten"); het datamodel is zo
/// opgezet dat meerdere organisaties later zonder migratie-pijn kunnen.
/// Implementeert bewust GEEN <see cref="ITenantEntiteit"/>: de organisatie
/// hoort niet bij zichzelf en valt buiten de globale tenant-queryfilter.
/// </summary>
public class Organisatie : Entiteit
{
    public required string Naam { get; set; }

    /// <summary>Nummer in het Landelijk Register Kinderopvang (LRK).</summary>
    public required string Lrknummer { get; set; }

    public ICollection<Stamgroep> Stamgroepen { get; set; } = new List<Stamgroep>();
    public ICollection<Kind> Kinderen { get; set; } = new List<Kind>();
    public ICollection<Medewerker> Medewerkers { get; set; } = new List<Medewerker>();
    public ICollection<Schoolvakantie> Schoolvakanties { get; set; } = new List<Schoolvakantie>();
}
