using KinderKompas.Application.Abstractions;
using KinderKompas.Application.Kinderen;
using KinderKompas.Domain.Autorisatie;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.ValueObjects;

namespace KinderKompas.Application.Tests.Privacy;

/// <summary>
/// Bewijst de privacy-kernregel: oudergegevens zijn alleen zichtbaar voor een
/// aanroeper met de capability <see cref="Capabilities.MagOudergegevensZien"/>
/// (Groepsportaal op locatie / Beheerder). Een thuis-medewerker krijgt ze niet.
/// </summary>
public class OudergegevensProjectieTests
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
    public void ThuisMedewerker_ZonderCapability_KrijgtGeenOudergegevens()
    {
        Kind kind = MaakKindMetOuder();
        var thuis = new FakeCurrentUser(/* geen capabilities */);

        KindDto dto = KindMapper.NaarDto(kind, thuis, Peildatum);

        Assert.Null(dto.Oudercontact);
        // De overige (niet-privacygevoelige) gegevens komen wél mee.
        Assert.Equal("Fenna", dto.Voornaam);
    }

    [Fact]
    public void Groepsportaal_MetCapability_KrijgtWelOudergegevens()
    {
        Kind kind = MaakKindMetOuder();
        var portaal = new FakeCurrentUser(Capabilities.MagOudergegevensZien);

        KindDto dto = KindMapper.NaarDto(kind, portaal, Peildatum);

        Assert.NotNull(dto.Oudercontact);
        Assert.Equal("Mark de Vries", dto.Oudercontact!.Naam);
        Assert.Equal("0612345678", dto.Oudercontact.Telefoon);
        Assert.Equal("mark@example.nl", dto.Oudercontact.Email);
    }

    [Fact]
    public void ZonderOudercontact_BlijftNull_OokMetCapability()
    {
        Kind kind = MaakKindMetOuder();
        kind.Oudercontact = null;
        var portaal = new FakeCurrentUser(Capabilities.MagOudergegevensZien);

        KindDto dto = KindMapper.NaarDto(kind, portaal, Peildatum);

        Assert.Null(dto.Oudercontact);
    }

    /// <summary>Test-dubbel voor de ingelogde gebruiker met een vaste set capabilities.</summary>
    private sealed class FakeCurrentUser : ICurrentUser
    {
        private readonly HashSet<string> _capabilities;

        public FakeCurrentUser(params string[] capabilities)
        {
            _capabilities = new HashSet<string>(capabilities);
        }

        public bool IsGeauthenticeerd => true;
        public string? UserId => "test-user";
        public Guid? OrganisatieId => Guid.Parse("0a000000-0000-0000-0000-000000000001");
        public Guid? MedewerkerId => null;
        public IReadOnlySet<string> Capabilities => _capabilities;
        public bool Heeft(string capability) => _capabilities.Contains(capability);
    }
}
