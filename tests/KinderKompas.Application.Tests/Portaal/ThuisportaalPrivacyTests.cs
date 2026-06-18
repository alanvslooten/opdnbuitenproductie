using System.Reflection;
using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Application.Portaal;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Tests.Portaal;

/// <summary>
/// Bewijst de privacy-scheiding van fase 8 HARD:
/// 1. Toegang tot het thuis-portaal (<see cref="Capabilities.MagThuisportaalGebruiken"/>)
///    geeft op zichzelf GEEN oudergegevens — die hangen aan een aparte capability.
/// 2. De thuis-portaal-DTO's bevatten per ONTWERP geen enkel veld dat oudergegevens
///    zou kunnen dragen: het lek kan dus niet eens per ongeluk ontstaan.
/// </summary>
public class ThuisportaalPrivacyTests
{
    private static readonly DateOnly Peildatum = new(2026, 6, 18);

    private static Kind MaakKindMetOuder() => new()
    {
        Voornaam = "Fenna",
        Achternaam = "de Vries",
        Geboortedatum = new DateOnly(2023, 4, 1),
        StamgroepId = Guid.NewGuid(),
        Startdatum = new DateOnly(2023, 7, 1),
        Oudercontact = new Oudercontact("Mark de Vries", "0612345678", "mark@example.nl"),
    };

    [Fact]
    public void ThuisportaalCapability_GeeftGeenOudergegevens()
    {
        Kind kind = MaakKindMetOuder();
        // Een medewerker mét thuis-portaal-toegang, maar zonder de oudergegevens-capability.
        var thuis = new FakeCurrentUser(Capabilities.MagThuisportaalGebruiken);

        KindDto dto = KindMapper.NaarDto(kind, thuis, Peildatum);

        Assert.Null(dto.Oudercontact);
    }

    [Fact]
    public void Groepsportaal_MetOudergegevensCapability_KrijgtZeWel()
    {
        Kind kind = MaakKindMetOuder();
        // Het groepsportaal op locatie heeft naast portaal-toegang ook de oudergegevens-capability.
        var portaal = new FakeCurrentUser(
            Capabilities.MagGroepsportaalGebruiken, Capabilities.MagOudergegevensZien);

        KindDto dto = KindMapper.NaarDto(kind, portaal, Peildatum);

        Assert.NotNull(dto.Oudercontact);
        Assert.Equal("Mark de Vries", dto.Oudercontact!.Naam);
    }

    [Fact]
    public void ThuisportaalDtos_BevattenGeenVeldVoorOudergegevens()
    {
        Type[] thuisDtos =
        {
            typeof(ThuisRoosterDto),
            typeof(ThuisRoosterDagDto),
            typeof(BeschikbaarheidDto),
            typeof(BeschikbaarheidInvoer),
            typeof(ThuisVerlofInvoer),
            typeof(UrenregistratieDto),
        };

        string[] verboden = { "ouder", "telefoon", "email", "oudercontact" };

        foreach (Type dto in thuisDtos)
        {
            foreach (PropertyInfo prop in dto.GetProperties())
            {
                string naam = prop.Name.ToLowerInvariant();
                Assert.DoesNotContain(verboden, term => naam.Contains(term));
                Assert.NotEqual(typeof(Oudercontact), prop.PropertyType);
                Assert.NotEqual(typeof(OudercontactDto), prop.PropertyType);
            }
        }
    }

    /// <summary>Test-dubbel voor de ingelogde gebruiker met een vaste set capabilities.</summary>
    private sealed class FakeCurrentUser : ICurrentUser
    {
        private readonly HashSet<string> _capabilities;

        public FakeCurrentUser(params string[] capabilities) => _capabilities = new HashSet<string>(capabilities);

        public bool IsGeauthenticeerd => true;
        public string? UserId => "test-user";
        public Guid? OrganisatieId => Guid.Parse("0a000000-0000-0000-0000-000000000001");
        public Guid? MedewerkerId => Guid.Parse("0b000000-0000-0000-0000-000000000002");
        public IReadOnlySet<string> Capabilities => _capabilities;
        public bool Heeft(string capability) => _capabilities.Contains(capability);
    }
}
