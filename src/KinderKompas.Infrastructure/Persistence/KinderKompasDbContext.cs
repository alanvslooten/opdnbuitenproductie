using KinderKompas.Application.Abstractions;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Common;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// De EF Core context voor KinderKompas. Sinds fase 3 ook de Identity-store
/// (erft van <see cref="IdentityDbContext{TUser}"/>). Verantwoordelijk voor:
/// 1. de tabel-mapping (code-first), inclusief de Identity-tabellen;
/// 2. de globale tenant-queryfilter, zodat queries automatisch alleen rijen van
///    de huidige organisatie zien;
/// 3. het automatisch zetten van OrganisatieId en de auditvelden bij opslaan.
/// </summary>
public class KinderKompasDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantProvider _tenantProvider;

    public KinderKompasDbContext(
        DbContextOptions<KinderKompasDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Organisatie> Organisaties => Set<Organisatie>();
    public DbSet<Stamgroep> Stamgroepen => Set<Stamgroep>();
    public DbSet<Kind> Kinderen => Set<Kind>();
    public DbSet<Medewerker> Medewerkers => Set<Medewerker>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<RolCapability> RolCapabilities => Set<RolCapability>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Schoolvakantie> Schoolvakanties => Set<Schoolvakantie>();
    public DbSet<Roosterweek> Roosterweken => Set<Roosterweek>();
    public DbSet<Roosterdienst> Roosterdiensten => Set<Roosterdienst>();
    public DbSet<Verlofaanvraag> Verlofaanvragen => Set<Verlofaanvraag>();
    public DbSet<Ziekmelding> Ziekmeldingen => Set<Ziekmelding>();
    public DbSet<Verlofsaldo> Verlofsaldi => Set<Verlofsaldo>();
    public DbSet<WachtlijstInschrijving> Wachtlijstinschrijvingen => Set<WachtlijstInschrijving>();
    public DbSet<Contact> Contacten => Set<Contact>();
    public DbSet<Rondleiding> Rondleidingen => Set<Rondleiding>();
    public DbSet<Voorstel> Voorstellen => Set<Voorstel>();
    public DbSet<VoorstelDag> VoorstelDagen => Set<VoorstelDag>();
    public DbSet<Observatie> Observaties => Set<Observatie>();
    public DbSet<Urenregistratie> Urenregistraties => Set<Urenregistratie>();
    public DbSet<Melding> Meldingen => Set<Melding>();
    public DbSet<OrganisatieInstellingen> OrganisatieInstellingen => Set<OrganisatieInstellingen>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // base regelt de Identity-tabellen (AspNetUsers, AspNetRoles, ...).
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organisatie>(b =>
        {
            b.Property(o => o.Naam).HasMaxLength(200).IsRequired();
            b.Property(o => o.Lrknummer).HasMaxLength(50).IsRequired();
            b.HasMany(o => o.Stamgroepen).WithOne(s => s.Organisatie!)
                .HasForeignKey(s => s.OrganisatieId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(o => o.Kinderen).WithOne(k => k.Organisatie!)
                .HasForeignKey(k => k.OrganisatieId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(o => o.Medewerkers).WithOne(m => m.Organisatie!)
                .HasForeignKey(m => m.OrganisatieId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(o => o.Schoolvakanties).WithOne(v => v.Organisatie!)
                .HasForeignKey(v => v.OrganisatieId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Stamgroep>(b =>
        {
            b.Property(s => s.Naam).HasMaxLength(100).IsRequired();
            b.HasMany(s => s.Kinderen).WithOne(k => k.Stamgroep!)
                .HasForeignKey(k => k.StamgroepId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Kind>(b =>
        {
            b.Property(k => k.Voornaam).HasMaxLength(100).IsRequired();
            b.Property(k => k.Achternaam).HasMaxLength(100).IsRequired();

            // Oudercontacten: lijst van owned value objects, opgeslagen als één JSON-
            // kolom ("Oudercontacten", jsonb). Zo kan een kind meerdere contacten hebben
            // zonder aparte tabel. Geen eigen identiteit; zichtbaarheid via projectie.
            b.OwnsMany(k => k.Oudercontacten, oc => oc.ToJson());

            // Optionele koppeling naar het CRM-contact (gezin); SetNull zodat het
            // verwijderen van een contact het kind niet meeneemt.
            b.HasOne(k => k.Contact).WithMany(c => c.Kinderen)
                .HasForeignKey(k => k.ContactId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Medewerker>(b =>
        {
            b.Property(m => m.Voornaam).HasMaxLength(100).IsRequired();
            b.Property(m => m.Achternaam).HasMaxLength(100).IsRequired();
            b.Property(m => m.Contracturen).HasPrecision(5, 2);
            b.Property(m => m.IdentityUserId).HasMaxLength(450);
            b.Property(m => m.Telefoon).HasMaxLength(30);
            b.Property(m => m.Email).HasMaxLength(200);
            b.Property(m => m.NoodcontactNaam).HasMaxLength(200);
            b.Property(m => m.NoodcontactTelefoon).HasMaxLength(30);

            // Vaste thuisgroep: optionele relatie. Restrict zodat een groep niet kan
            // verdwijnen terwijl er medewerkers aan hangen.
            b.HasOne(m => m.VasteStamgroep).WithMany()
                .HasForeignKey(m => m.VasteStamgroepId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Capability>(b =>
        {
            b.Property(c => c.Sleutel).HasMaxLength(100).IsRequired();
            b.Property(c => c.Omschrijving).HasMaxLength(300).IsRequired();
            b.HasIndex(c => c.Sleutel).IsUnique();
            b.HasMany(c => c.RolCapabilities).WithOne(rc => rc.Capability!)
                .HasForeignKey(rc => rc.CapabilityId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolCapability>(b =>
        {
            // Binnen één organisatie is elke (Rol, Capability)-combinatie uniek.
            b.HasIndex(rc => new { rc.OrganisatieId, rc.Rol, rc.CapabilityId }).IsUnique();
        });

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(u => u.OrganisatieId);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasOne(t => t.ApplicationUser).WithMany()
                .HasForeignKey(t => t.ApplicationUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Schoolvakantie>(b =>
        {
            b.Property(v => v.Naam).HasMaxLength(100).IsRequired();
            // Vakanties worden per schooljaar opgevraagd; index helpt die query.
            b.HasIndex(v => new { v.OrganisatieId, v.Schooljaar });
        });

        modelBuilder.Entity<Roosterweek>(b =>
        {
            // Eén roosterweek per organisatie per maandag.
            b.HasIndex(r => new { r.OrganisatieId, r.WeekBegin }).IsUnique();
            b.HasMany(r => r.Diensten).WithOne(d => d.Roosterweek!)
                .HasForeignKey(d => d.RoosterweekId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Roosterdienst>(b =>
        {
            b.Property(d => d.Taakomschrijving).HasMaxLength(500);
            // Een medewerker staat per dag maximaal één keer in dezelfde groep.
            b.HasIndex(d => new { d.MedewerkerId, d.Datum, d.StamgroepId }).IsUnique();
            b.HasOne(d => d.Medewerker).WithMany()
                .HasForeignKey(d => d.MedewerkerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(d => d.Stamgroep).WithMany()
                .HasForeignKey(d => d.StamgroepId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Verlofaanvraag>(b =>
        {
            b.Property(v => v.AantalUren).HasPrecision(6, 2);
            b.Property(v => v.Reden).HasMaxLength(500);
            b.Property(v => v.BeoordelingsNotitie).HasMaxLength(500);
            // Het archief filtert op status; index helpt die query.
            b.HasIndex(v => new { v.OrganisatieId, v.Status });
            b.HasOne(v => v.Medewerker).WithMany()
                .HasForeignKey(v => v.MedewerkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ziekmelding>(b =>
        {
            b.HasIndex(z => new { z.OrganisatieId, z.MedewerkerId });
            b.HasOne(z => z.Medewerker).WithMany()
                .HasForeignKey(z => z.MedewerkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Verlofsaldo>(b =>
        {
            b.Property(s => s.ToegekendeUren).HasPrecision(6, 2);
            // Eén saldo per medewerker per categorie.
            b.HasIndex(s => new { s.MedewerkerId, s.Categorie }).IsUnique();
            b.HasOne(s => s.Medewerker).WithMany()
                .HasForeignKey(s => s.MedewerkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WachtlijstInschrijving>(b =>
        {
            b.Property(w => w.Voornaam).HasMaxLength(100).IsRequired();
            b.Property(w => w.Achternaam).HasMaxLength(100).IsRequired();
            b.Property(w => w.Notitie).HasMaxLength(1000);

            // Oudercontact als owned value object — zelfde patroon als bij Kind.
            b.OwnsOne(w => w.Oudercontact, oc =>
            {
                oc.Property(o => o.Naam).HasColumnName("Oudercontact_Naam").HasMaxLength(200);
                oc.Property(o => o.Telefoon).HasColumnName("Oudercontact_Telefoon").HasMaxLength(30);
                oc.Property(o => o.Email).HasColumnName("Oudercontact_Email").HasMaxLength(200);
            });

            // Optionele voorkeursgroep; Restrict zodat een groep niet verdwijnt
            // terwijl er wachtlijstkinderen naar verwijzen.
            b.HasOne(w => w.GewensteStamgroep).WithMany()
                .HasForeignKey(w => w.GewensteStamgroepId).OnDelete(DeleteBehavior.Restrict);

            // Expliciete tenant-relatie (Restrict), zonder back-collection op Organisatie.
            b.HasOne(w => w.Organisatie).WithMany()
                .HasForeignKey(w => w.OrganisatieId).OnDelete(DeleteBehavior.Restrict);

            b.HasMany(w => w.Voorstellen).WithOne(v => v.WachtlijstInschrijving!)
                .HasForeignKey(v => v.WachtlijstInschrijvingId).OnDelete(DeleteBehavior.Cascade);

            // Optionele koppeling naar het CRM-contact (gezin); SetNull bij verwijderen.
            b.HasOne(w => w.Contact).WithMany(c => c.Inschrijvingen)
                .HasForeignKey(w => w.ContactId).OnDelete(DeleteBehavior.SetNull);

            // De wachtlijst wordt gefilterd op status (wachtend/geplaatst); index helpt.
            b.HasIndex(w => new { w.OrganisatieId, w.Status });
        });

        modelBuilder.Entity<Contact>(b =>
        {
            b.Property(c => c.Voornaam).HasMaxLength(100).IsRequired();
            b.Property(c => c.Achternaam).HasMaxLength(100).IsRequired();
            b.Property(c => c.Telefoon).HasMaxLength(30);
            b.Property(c => c.Email).HasMaxLength(200);
            b.Property(c => c.Aantekeningen).HasMaxLength(2000);

            b.HasOne(c => c.Organisatie).WithMany()
                .HasForeignKey(c => c.OrganisatieId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(c => c.Rondleidingen).WithOne(r => r.Contact!)
                .HasForeignKey(r => r.ContactId).OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(c => new { c.OrganisatieId, c.Achternaam });
        });

        modelBuilder.Entity<Rondleiding>(b =>
        {
            b.Property(r => r.Notitie).HasMaxLength(1000);
            b.HasIndex(r => r.ContactId);
        });

        modelBuilder.Entity<Voorstel>(b =>
        {
            b.Property(v => v.Notitie).HasMaxLength(1000);
            b.HasOne(v => v.VoorgesteldeStamgroep).WithMany()
                .HasForeignKey(v => v.VoorgesteldeStamgroepId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(v => v.Dagen).WithOne(d => d.Voorstel!)
                .HasForeignKey(d => d.VoorstelId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(v => v.WachtlijstInschrijvingId);
        });

        modelBuilder.Entity<VoorstelDag>(b =>
        {
            // Eén voorgestelde dag per weekdag binnen een voorstel.
            b.HasIndex(d => new { d.VoorstelId, d.Weekdag }).IsUnique();
        });

        modelBuilder.Entity<Observatie>(b =>
        {
            b.Property(o => o.BestandsNaam).HasMaxLength(260).IsRequired();
            b.Property(o => o.BestandsSleutel).HasMaxLength(400).IsRequired();
            b.Property(o => o.ContentType).HasMaxLength(100).IsRequired();
            b.Property(o => o.VerzondenNaarEmail).HasMaxLength(200);

            // Eén observatie per kind per mijlpaal: afvinken is idempotent per moment.
            b.HasIndex(o => new { o.KindId, o.MijlpaalMaanden }).IsUnique();

            // Verwijdert het kind → ook zijn observatie-rijen (de PDF-bestanden zelf
            // worden in de use-case opgeruimd; cascade ruimt alleen de metadata).
            b.HasOne(o => o.Kind).WithMany()
                .HasForeignKey(o => o.KindId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Urenregistratie>(b =>
        {
            // De dag-/medewerker-query (eigen uren, dienst van de dag) leunt op deze index.
            b.HasIndex(u => new { u.OrganisatieId, u.MedewerkerId, u.Datum });

            b.HasOne(u => u.Medewerker).WithMany()
                .HasForeignKey(u => u.MedewerkerId).OnDelete(DeleteBehavior.Restrict);
            // Verdwijnt de geplande dienst, dan blijft de werkelijke registratie staan
            // (de link wordt null): werkelijke uren mogen niet zomaar verdampen.
            b.HasOne(u => u.Roosterdienst).WithMany()
                .HasForeignKey(u => u.RoosterdienstId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(u => u.Stamgroep).WithMany()
                .HasForeignKey(u => u.StamgroepId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Melding>(b =>
        {
            b.Property(m => m.Titel).HasMaxLength(200).IsRequired();
            b.Property(m => m.Tekst).HasMaxLength(1000).IsRequired();
            b.Property(m => m.BronType).HasMaxLength(100);
            b.Property(m => m.DeduplicatieSleutel).HasMaxLength(200);

            // Het actiecentrum filtert op status (open vs afgehandeld) en sorteert op tijd;
            // de dispatcher zoekt op (org, sleutel, status) voor de dedup-lookup.
            b.HasIndex(m => new { m.OrganisatieId, m.Status });
            b.HasIndex(m => new { m.OrganisatieId, m.DeduplicatieSleutel, m.Status });
        });

        modelBuilder.Entity<OrganisatieInstellingen>(b =>
        {
            b.Property(i => i.VerborgenMeldingsoorten).HasMaxLength(100).IsRequired();
            b.Property(i => i.StandaardObservatietekst).HasMaxLength(4000);
            // Precies één instellingen-rij per organisatie.
            b.HasIndex(i => i.OrganisatieId).IsUnique();
        });

        // Globale tenant-queryfilter: elke entiteit die ITenantEntiteit
        // implementeert wordt automatisch gefilterd op de huidige organisatie.
        // De Organisatie zelf valt hier bewust buiten. De Identity-tabellen
        // (incl. ApplicationUser) ook: auth-lookups gebeuren vóór de tenant
        // bekend is, dus die scopen we expliciet in de queries.
        modelBuilder.Entity<Stamgroep>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Kind>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Medewerker>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<RolCapability>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Schoolvakantie>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Roosterweek>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Roosterdienst>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Verlofaanvraag>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Ziekmelding>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Verlofsaldo>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<WachtlijstInschrijving>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Contact>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Rondleiding>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Voorstel>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<VoorstelDag>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Observatie>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Urenregistratie>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<Melding>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);
        modelBuilder.Entity<OrganisatieInstellingen>().HasQueryFilter(e => e.OrganisatieId == _tenantProvider.CurrentOrganisatieId);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organisatie>().HasData(new
        {
            Id = SeedConstanten.OrganisatieId,
            Naam = "Op d'n Buiten",
            Lrknummer = "000000000",
            AangemaaktOp = SeedConstanten.SeedMoment,
            GewijzigdOp = SeedConstanten.SeedMoment
        });

        modelBuilder.Entity<Stamgroep>().HasData(
            new
            {
                Id = SeedConstanten.StamgroepBengeltjesId,
                OrganisatieId = SeedConstanten.OrganisatieId,
                Naam = "Bengeltjes",
                MaxKinderen = 12,
                AangemaaktOp = SeedConstanten.SeedMoment,
                GewijzigdOp = SeedConstanten.SeedMoment
            },
            new
            {
                Id = SeedConstanten.StamgroepBoefjesId,
                OrganisatieId = SeedConstanten.OrganisatieId,
                Naam = "Boefjes",
                MaxKinderen = 12,
                AangemaaktOp = SeedConstanten.SeedMoment,
                GewijzigdOp = SeedConstanten.SeedMoment
            });

        modelBuilder.Entity<OrganisatieInstellingen>().HasData(new
        {
            Id = SeedConstanten.OrganisatieInstellingenId,
            OrganisatieId = SeedConstanten.OrganisatieId,
            VerborgenMeldingsoorten = "",
            ObservatieBinnenkortDrempelDagen = Domain.Services.Observatieschema.StandaardBinnenkortDrempelDagen,
            KindBinnenkortVierDrempelDagen = Domain.Entiteiten.OrganisatieInstellingen.StandaardKindBinnenkortVierDagen,
            PrioriteitInternGewicht = Domain.Services.WachtlijstPrioriteit.PuntenIntern,
            PrioriteitPerMaandGewicht = Domain.Services.WachtlijstPrioriteit.PuntenPerMaandWachtend,
            AangemaaktOp = SeedConstanten.SeedMoment,
            GewijzigdOp = SeedConstanten.SeedMoment
        });

        SeedAutorisatie(modelBuilder);
    }

    /// <summary>
    /// Seedt de capability-referentietabel en de default rechten-mapping voor de
    /// seed-organisatie. GUID's zijn deterministisch afgeleid van de sleutel,
    /// zodat de migratie stabiel blijft.
    /// </summary>
    private static void SeedAutorisatie(ModelBuilder modelBuilder)
    {
        foreach (CapabilityDefinitie def in Domain.Autorisatie.Capabilities.Alle)
        {
            modelBuilder.Entity<Capability>().HasData(new
            {
                Id = DeterministischeGuid.Maak($"cap:{def.Sleutel}"),
                Sleutel = def.Sleutel,
                Omschrijving = def.Omschrijving,
                AangemaaktOp = SeedConstanten.SeedMoment,
                GewijzigdOp = SeedConstanten.SeedMoment
            });
        }

        foreach ((var rol, string[] sleutels) in StandaardRolCapabilities.Standaard)
        {
            foreach (string sleutel in sleutels)
            {
                modelBuilder.Entity<RolCapability>().HasData(new
                {
                    Id = DeterministischeGuid.Maak($"rolcap:{SeedConstanten.OrganisatieId}:{rol}:{sleutel}"),
                    OrganisatieId = SeedConstanten.OrganisatieId,
                    Rol = rol,
                    CapabilityId = DeterministischeGuid.Maak($"cap:{sleutel}"),
                    AangemaaktOp = SeedConstanten.SeedMoment,
                    GewijzigdOp = SeedConstanten.SeedMoment
                });
            }
        }
    }

    public override int SaveChanges()
    {
        ZetTenantEnAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ZetTenantEnAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Zet vóór elke opslag automatisch de tenant-sleutel op nieuwe entiteiten
    /// en werkt de auditvelden bij. Zo hoeft business-logica dit nooit zelf te
    /// doen en kan OrganisatieId niet per ongeluk verkeerd of leeg blijven.
    /// </summary>
    private void ZetTenantEnAudit()
    {
        DateTime nu = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entiteit>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is ITenantEntiteit nieuweTenant &&
                        nieuweTenant.OrganisatieId == Guid.Empty)
                    {
                        nieuweTenant.OrganisatieId = _tenantProvider.CurrentOrganisatieId;
                    }
                    entry.Entity.AangemaaktOp = nu;
                    entry.Entity.GewijzigdOp = nu;
                    break;

                case EntityState.Modified:
                    entry.Entity.GewijzigdOp = nu;
                    // AangemaaktOp mag nooit wijzigen.
                    entry.Property(e => e.AangemaaktOp).IsModified = false;
                    break;
            }
        }
    }
}
