using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initieel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sleutel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Omschrijving = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meldingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Soort = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VereistActie = table.Column<bool>(type: "boolean", nullable: false),
                    Titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tekst = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BronType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BronId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeduplicatieSleutel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AfgehandeldOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meldingen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganisatieInstellingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VerborgenMeldingsoorten = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ObservatieBinnenkortDrempelDagen = table.Column<int>(type: "integer", nullable: false),
                    KindBinnenkortVierDrempelDagen = table.Column<int>(type: "integer", nullable: false),
                    StandaardObservatietekst = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PrioriteitInternGewicht = table.Column<int>(type: "integer", nullable: false),
                    PrioriteitPerMaandGewicht = table.Column<int>(type: "integer", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisatieInstellingen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organisaties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Lrknummer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisaties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roosterweken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekBegin = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VerstuurdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roosterweken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VerlooptOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IngetrokkenOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VervangenDoorTokenHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolCapabilities_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Schoolvakanties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Schooljaar = table.Column<int>(type: "integer", nullable: false),
                    Begindatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schoolvakanties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schoolvakanties_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stamgroepen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Naam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxKinderen = table.Column<int>(type: "integer", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stamgroepen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stamgroepen_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medewerkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Voornaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    VasteWerkdagen = table.Column<int>(type: "integer", nullable: false),
                    Beschikbaarheidsdagen = table.Column<int>(type: "integer", nullable: false),
                    Contracturen = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    VasteStamgroepId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdentityUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medewerkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medewerkers_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medewerkers_Stamgroepen_VasteStamgroepId",
                        column: x => x.VasteStamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wachtlijstinschrijvingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Voornaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Geboortedatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Oudercontact_Naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Oudercontact_Telefoon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Oudercontact_Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InschrijfdatumWachtlijst = table.Column<DateOnly>(type: "date", nullable: false),
                    GewensteStartdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    GewensteOpvangdagen = table.Column<int>(type: "integer", nullable: false),
                    Contracttype = table.Column<int>(type: "integer", nullable: false),
                    GewensteStamgroepId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsIntern = table.Column<bool>(type: "boolean", nullable: false),
                    HandmatigBovenaan = table.Column<bool>(type: "boolean", nullable: false),
                    ReedsGeplaatsteDagen = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notitie = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wachtlijstinschrijvingen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wachtlijstinschrijvingen_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wachtlijstinschrijvingen_Stamgroepen_GewensteStamgroepId",
                        column: x => x.GewensteStamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kinderen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Voornaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Geboortedatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Oudercontact_Naam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Oudercontact_Telefoon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Oudercontact_Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StamgroepId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Startdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: true),
                    Contracttype = table.Column<int>(type: "integer", nullable: false),
                    GewensteOpvangdagen = table.Column<int>(type: "integer", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinderen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kinderen_Medewerkers_MentorId",
                        column: x => x.MentorId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Kinderen_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kinderen_Stamgroepen_StamgroepId",
                        column: x => x.StamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roosterdiensten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoosterweekId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StamgroepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Taakomschrijving = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UrencorrectieKwartieren = table.Column<int>(type: "integer", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roosterdiensten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roosterdiensten_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roosterdiensten_Roosterweken_RoosterweekId",
                        column: x => x.RoosterweekId,
                        principalTable: "Roosterweken",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Roosterdiensten_Stamgroepen_StamgroepId",
                        column: x => x.StamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Verlofaanvragen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Begindatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: false),
                    AantalUren = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Categorie = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reden = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BeoordelingsNotitie = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BeoordeeldOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verlofaanvragen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verlofaanvragen_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Verlofsaldi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Categorie = table.Column<int>(type: "integer", nullable: false),
                    ToegekendeUren = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Vervaldatum = table.Column<DateOnly>(type: "date", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verlofsaldi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verlofsaldi_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ziekmeldingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Begindatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ziekmeldingen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ziekmeldingen_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voorstellen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WachtlijstInschrijvingId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerstuurdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VoorgesteldeStamgroepId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoorgesteldeDagen = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BeantwoordOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notitie = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voorstellen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voorstellen_Stamgroepen_VoorgesteldeStamgroepId",
                        column: x => x.VoorgesteldeStamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voorstellen_Wachtlijstinschrijvingen_WachtlijstInschrijving~",
                        column: x => x.WachtlijstInschrijvingId,
                        principalTable: "Wachtlijstinschrijvingen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Observaties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: false),
                    MijlpaalMaanden = table.Column<int>(type: "integer", nullable: false),
                    BestandsNaam = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    BestandsSleutel = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BestandsGrootte = table.Column<long>(type: "bigint", nullable: false),
                    VerzondenOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VerzondenNaarEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observaties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observaties_Kinderen_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinderen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Urenregistraties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoosterdienstId = table.Column<Guid>(type: "uuid", nullable: true),
                    StamgroepId = table.Column<Guid>(type: "uuid", nullable: true),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Ingeklokt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Uitgeklokt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urenregistraties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Urenregistraties_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Urenregistraties_Roosterdiensten_RoosterdienstId",
                        column: x => x.RoosterdienstId,
                        principalTable: "Roosterdiensten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Urenregistraties_Stamgroepen_StamgroepId",
                        column: x => x.StamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoorstelDagen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoorstelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weekdag = table.Column<int>(type: "integer", nullable: false),
                    VoorgesteldeDatum = table.Column<DateOnly>(type: "date", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoorstelDagen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoorstelDagen_Voorstellen_VoorstelId",
                        column: x => x.VoorstelId,
                        principalTable: "Voorstellen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Capabilities",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Omschrijving", "Sleutel" },
                values: new object[,]
                {
                    { new Guid("533ca9a8-caf9-b9ff-3410-4d086e50b08d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oudergegevens (contact, telefoon, mail) inzien", "MagOudergegevensZien" },
                    { new Guid("59cb0af1-81b4-eebd-aec1-06149fbd3c1a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Werkrooster opstellen en wijzigen", "MagRoosterBeheren" },
                    { new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kindgegevens en plaatsing beheren", "MagKinderenBeheren" },
                    { new Guid("88520ca6-7c25-d792-7ea1-7f5c98503f0a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Werkrooster definitief versturen", "MagRoosterVersturen" },
                    { new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thuis-portaal gebruiken (eigen rooster, beschikbaarheid, verlof)", "MagThuisportaalGebruiken" },
                    { new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dashboard en het actiecentrum (meldingen/to-do's) inzien", "MagDashboardZien" },
                    { new Guid("d964b02e-1fb6-4050-a044-13488445cf14"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Groepsportaal op locatie gebruiken (inklokken, dienst, observaties)", "MagGroepsportaalGebruiken" },
                    { new Guid("e85f93b6-c03c-c213-0e21-1c349581f502"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Medewerkers en hun rollen beheren", "MagMedewerkersBeheren" },
                    { new Guid("e904ca58-9b3a-74e1-b988-6d927c70b5b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Wachtlijst en plaatsingsvoorstellen beheren", "MagWachtlijstBeheren" },
                    { new Guid("f45086d0-7b9a-5867-6dfa-55825b170364"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Organisatie-instellingen en rechten beheren", "MagInstellingenBeheren" },
                    { new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Observaties opstellen en versturen", "MagObservatiesVersturen" }
                });

            migrationBuilder.InsertData(
                table: "OrganisatieInstellingen",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "KindBinnenkortVierDrempelDagen", "ObservatieBinnenkortDrempelDagen", "OrganisatieId", "PrioriteitInternGewicht", "PrioriteitPerMaandGewicht", "StandaardObservatietekst", "VerborgenMeldingsoorten" },
                values: new object[] { new Guid("0c000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, 30, new Guid("0a000000-0000-0000-0000-000000000001"), 500, 10, null, "" });

            migrationBuilder.InsertData(
                table: "Organisaties",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Lrknummer", "Naam" },
                values: new object[] { new Guid("0a000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "000000000", "Op d'n Buiten" });

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[,]
                {
                    { new Guid("02f2fcc8-427c-97cd-4bef-06daa8cc12ac"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("59cb0af1-81b4-eebd-aec1-06149fbd3c1a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("0649583d-7fc9-ae53-0505-c40006580cf8"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e85f93b6-c03c-c213-0e21-1c349581f502"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("0b94099f-7e05-39fe-f3b3-ff660a5c7652"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 },
                    { new Guid("1a51ec29-a7a0-a0c9-fd46-8916cda2b2e4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d964b02e-1fb6-4050-a044-13488445cf14"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 },
                    { new Guid("1b5d041c-b612-dae1-3aec-e4dca2f9b037"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("20229c8a-a9c1-91c6-d80a-96c108b43ed5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("208206c0-1691-8ede-fc6b-82df6f71fd83"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 3 },
                    { new Guid("217c664d-eee1-1a26-1251-8b54660d2236"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("88520ca6-7c25-d792-7ea1-7f5c98503f0a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("22f8dc75-db51-0230-d19f-5184a6a1dfaa"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("533ca9a8-caf9-b9ff-3410-4d086e50b08d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("24b677db-c986-9e9e-5952-8bd6982fe112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("59cb0af1-81b4-eebd-aec1-06149fbd3c1a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("2ed6d771-6072-52ca-ea4a-6ea9db12baf7"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e85f93b6-c03c-c213-0e21-1c349581f502"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("475575ef-c7cc-a022-28db-3a0ede0ef554"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f45086d0-7b9a-5867-6dfa-55825b170364"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("4a1965fd-86dd-15a8-22f4-8fb8371d46b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 3 },
                    { new Guid("4e41907a-bedf-f19f-78b6-cb3ef1247215"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("5a808c6b-f30f-cc48-b56e-687e90a218d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("88520ca6-7c25-d792-7ea1-7f5c98503f0a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("5cd31be3-00a0-68fa-2e95-25ae6c52c852"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e904ca58-9b3a-74e1-b988-6d927c70b5b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("6a45542c-1ee8-400e-823e-cbbb3db4a8c0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("6a788f24-3700-1dac-8655-7c586ab46755"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("533ca9a8-caf9-b9ff-3410-4d086e50b08d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 },
                    { new Guid("6fd6fc2a-11c0-54c5-3cd1-23e8b5a0544c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("73078a48-aba9-4a1d-8698-648d0c815877"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e904ca58-9b3a-74e1-b988-6d927c70b5b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("85cc444f-b664-1442-09c0-9cba249f8327"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("e904ca58-9b3a-74e1-b988-6d927c70b5b2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("8e76cde6-0e4d-0ba1-f541-d9a82152ea89"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("9e03dfcb-b126-7d7b-3696-71d43494516d"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("b8faed65-f228-2629-8cd8-61b654fde6cf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("c16760ac-c001-6449-aa62-437ea5c2ae74"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("c2dcaad8-24e7-5675-a3de-6e32ebf9c98b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 },
                    { new Guid("c3ec8c6c-c4be-f859-4a01-f50f7e2eba57"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("88520ca6-7c25-d792-7ea1-7f5c98503f0a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("ee1fe437-9a79-ddb4-30a3-ccd4526b9866"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fe90a764-2947-895f-6ba8-2184df6e59c5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("f9953744-0012-bc39-80e1-d0df5dd77c2f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("ff2fea9e-9489-1dd6-74cb-c2e447f8ad92"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 }
                });

            migrationBuilder.InsertData(
                table: "Stamgroepen",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "MaxKinderen", "Naam", "OrganisatieId" },
                values: new object[,]
                {
                    { new Guid("0b000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, "Bengeltjes", new Guid("0a000000-0000-0000-0000-000000000001") },
                    { new Guid("0b000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, "Boefjes", new Guid("0a000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganisatieId",
                table: "AspNetUsers",
                column: "OrganisatieId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_Sleutel",
                table: "Capabilities",
                column: "Sleutel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kinderen_MentorId",
                table: "Kinderen",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_Kinderen_OrganisatieId",
                table: "Kinderen",
                column: "OrganisatieId");

            migrationBuilder.CreateIndex(
                name: "IX_Kinderen_StamgroepId",
                table: "Kinderen",
                column: "StamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Medewerkers_OrganisatieId",
                table: "Medewerkers",
                column: "OrganisatieId");

            migrationBuilder.CreateIndex(
                name: "IX_Medewerkers_VasteStamgroepId",
                table: "Medewerkers",
                column: "VasteStamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Meldingen_OrganisatieId_DeduplicatieSleutel_Status",
                table: "Meldingen",
                columns: new[] { "OrganisatieId", "DeduplicatieSleutel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Meldingen_OrganisatieId_Status",
                table: "Meldingen",
                columns: new[] { "OrganisatieId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Observaties_KindId_MijlpaalMaanden",
                table: "Observaties",
                columns: new[] { "KindId", "MijlpaalMaanden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganisatieInstellingen_OrganisatieId",
                table: "OrganisatieInstellingen",
                column: "OrganisatieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ApplicationUserId",
                table: "RefreshTokens",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolCapabilities_CapabilityId",
                table: "RolCapabilities",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_RolCapabilities_OrganisatieId_Rol_CapabilityId",
                table: "RolCapabilities",
                columns: new[] { "OrganisatieId", "Rol", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roosterdiensten_MedewerkerId_Datum_StamgroepId",
                table: "Roosterdiensten",
                columns: new[] { "MedewerkerId", "Datum", "StamgroepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roosterdiensten_RoosterweekId",
                table: "Roosterdiensten",
                column: "RoosterweekId");

            migrationBuilder.CreateIndex(
                name: "IX_Roosterdiensten_StamgroepId",
                table: "Roosterdiensten",
                column: "StamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Roosterweken_OrganisatieId_WeekBegin",
                table: "Roosterweken",
                columns: new[] { "OrganisatieId", "WeekBegin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schoolvakanties_OrganisatieId_Schooljaar",
                table: "Schoolvakanties",
                columns: new[] { "OrganisatieId", "Schooljaar" });

            migrationBuilder.CreateIndex(
                name: "IX_Stamgroepen_OrganisatieId",
                table: "Stamgroepen",
                column: "OrganisatieId");

            migrationBuilder.CreateIndex(
                name: "IX_Urenregistraties_MedewerkerId",
                table: "Urenregistraties",
                column: "MedewerkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Urenregistraties_OrganisatieId_MedewerkerId_Datum",
                table: "Urenregistraties",
                columns: new[] { "OrganisatieId", "MedewerkerId", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_Urenregistraties_RoosterdienstId",
                table: "Urenregistraties",
                column: "RoosterdienstId");

            migrationBuilder.CreateIndex(
                name: "IX_Urenregistraties_StamgroepId",
                table: "Urenregistraties",
                column: "StamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Verlofaanvragen_MedewerkerId",
                table: "Verlofaanvragen",
                column: "MedewerkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Verlofaanvragen_OrganisatieId_Status",
                table: "Verlofaanvragen",
                columns: new[] { "OrganisatieId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Verlofsaldi_MedewerkerId_Categorie",
                table: "Verlofsaldi",
                columns: new[] { "MedewerkerId", "Categorie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoorstelDagen_VoorstelId_Weekdag",
                table: "VoorstelDagen",
                columns: new[] { "VoorstelId", "Weekdag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voorstellen_VoorgesteldeStamgroepId",
                table: "Voorstellen",
                column: "VoorgesteldeStamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Voorstellen_WachtlijstInschrijvingId",
                table: "Voorstellen",
                column: "WachtlijstInschrijvingId");

            migrationBuilder.CreateIndex(
                name: "IX_Wachtlijstinschrijvingen_GewensteStamgroepId",
                table: "Wachtlijstinschrijvingen",
                column: "GewensteStamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Wachtlijstinschrijvingen_OrganisatieId_Status",
                table: "Wachtlijstinschrijvingen",
                columns: new[] { "OrganisatieId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Ziekmeldingen_MedewerkerId",
                table: "Ziekmeldingen",
                column: "MedewerkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ziekmeldingen_OrganisatieId_MedewerkerId",
                table: "Ziekmeldingen",
                columns: new[] { "OrganisatieId", "MedewerkerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Meldingen");

            migrationBuilder.DropTable(
                name: "Observaties");

            migrationBuilder.DropTable(
                name: "OrganisatieInstellingen");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolCapabilities");

            migrationBuilder.DropTable(
                name: "Schoolvakanties");

            migrationBuilder.DropTable(
                name: "Urenregistraties");

            migrationBuilder.DropTable(
                name: "Verlofaanvragen");

            migrationBuilder.DropTable(
                name: "Verlofsaldi");

            migrationBuilder.DropTable(
                name: "VoorstelDagen");

            migrationBuilder.DropTable(
                name: "Ziekmeldingen");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Kinderen");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Capabilities");

            migrationBuilder.DropTable(
                name: "Roosterdiensten");

            migrationBuilder.DropTable(
                name: "Voorstellen");

            migrationBuilder.DropTable(
                name: "Medewerkers");

            migrationBuilder.DropTable(
                name: "Roosterweken");

            migrationBuilder.DropTable(
                name: "Wachtlijstinschrijvingen");

            migrationBuilder.DropTable(
                name: "Stamgroepen");

            migrationBuilder.DropTable(
                name: "Organisaties");
        }
    }
}
