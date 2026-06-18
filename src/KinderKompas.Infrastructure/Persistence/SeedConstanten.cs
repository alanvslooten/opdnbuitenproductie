namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Vaste identifiers en peildatum voor de seed-data. Bewust hard gecodeerd en
/// deterministisch: EF Core HasData vereist statische waarden (geen
/// Guid.NewGuid of DateTime.Now), anders genereert elke migratie verschil.
/// </summary>
public static class SeedConstanten
{
    /// <summary>De enige actieve organisatie in deze fase: "Op d'n Buiten".</summary>
    public static readonly Guid OrganisatieId = Guid.Parse("0a000000-0000-0000-0000-000000000001");

    public static readonly Guid StamgroepBengeltjesId = Guid.Parse("0b000000-0000-0000-0000-000000000001");
    public static readonly Guid StamgroepBoefjesId = Guid.Parse("0b000000-0000-0000-0000-000000000002");

    /// <summary>De instellingen-rij van de seed-organisatie (fase 9c).</summary>
    public static readonly Guid OrganisatieInstellingenId = Guid.Parse("0c000000-0000-0000-0000-000000000001");

    /// <summary>Vaste timestamp voor seed-records (UTC), zodat migraties stabiel zijn.</summary>
    public static readonly DateTime SeedMoment = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
