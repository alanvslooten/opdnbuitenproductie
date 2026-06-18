using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WachtlijstEnPersoneelsschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Beschikbaarheidsdagen",
                table: "Medewerkers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VasteStamgroepId",
                table: "Medewerkers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MentorId",
                table: "Kinderen",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MentorMedewerkerId",
                table: "Kinderen",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roosterweken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekBegin = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerstuurdOp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roosterweken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Verlofaanvragen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Begindatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: false),
                    AantalUren = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Categorie = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reden = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BeoordelingsNotitie = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BeoordeeldOp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categorie = table.Column<int>(type: "int", nullable: false),
                    ToegekendeUren = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Vervaldatum = table.Column<DateOnly>(type: "date", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Wachtlijstinschrijvingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Voornaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Geboortedatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Oudercontact_Naam = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Oudercontact_Telefoon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Oudercontact_Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InschrijfdatumWachtlijst = table.Column<DateOnly>(type: "date", nullable: false),
                    GewensteStartdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    GewensteOpvangdagen = table.Column<int>(type: "int", nullable: false),
                    Contracttype = table.Column<int>(type: "int", nullable: false),
                    GewensteStamgroepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsIntern = table.Column<bool>(type: "bit", nullable: false),
                    HandmatigBovenaan = table.Column<bool>(type: "bit", nullable: false),
                    ReedsGeplaatsteDagen = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notitie = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Ziekmeldingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Begindatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Roosterdiensten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoosterweekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StamgroepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Taakomschrijving = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UrencorrectieKwartieren = table.Column<int>(type: "int", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Voorstellen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WachtlijstInschrijvingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerstuurdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoorgesteldeStamgroepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoorgesteldeDagen = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BeantwoordOp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notitie = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                        name: "FK_Voorstellen_Wachtlijstinschrijvingen_WachtlijstInschrijvingId",
                        column: x => x.WachtlijstInschrijvingId,
                        principalTable: "Wachtlijstinschrijvingen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoorstelDagen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoorstelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weekdag = table.Column<int>(type: "int", nullable: false),
                    VoorgesteldeDatum = table.Column<DateOnly>(type: "date", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Medewerkers_VasteStamgroepId",
                table: "Medewerkers",
                column: "VasteStamgroepId");

            migrationBuilder.CreateIndex(
                name: "IX_Kinderen_MentorId",
                table: "Kinderen",
                column: "MentorId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Kinderen_Medewerkers_MentorId",
                table: "Kinderen",
                column: "MentorId",
                principalTable: "Medewerkers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medewerkers_Stamgroepen_VasteStamgroepId",
                table: "Medewerkers",
                column: "VasteStamgroepId",
                principalTable: "Stamgroepen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kinderen_Medewerkers_MentorId",
                table: "Kinderen");

            migrationBuilder.DropForeignKey(
                name: "FK_Medewerkers_Stamgroepen_VasteStamgroepId",
                table: "Medewerkers");

            migrationBuilder.DropTable(
                name: "Roosterdiensten");

            migrationBuilder.DropTable(
                name: "Verlofaanvragen");

            migrationBuilder.DropTable(
                name: "Verlofsaldi");

            migrationBuilder.DropTable(
                name: "VoorstelDagen");

            migrationBuilder.DropTable(
                name: "Ziekmeldingen");

            migrationBuilder.DropTable(
                name: "Roosterweken");

            migrationBuilder.DropTable(
                name: "Voorstellen");

            migrationBuilder.DropTable(
                name: "Wachtlijstinschrijvingen");

            migrationBuilder.DropIndex(
                name: "IX_Medewerkers_VasteStamgroepId",
                table: "Medewerkers");

            migrationBuilder.DropIndex(
                name: "IX_Kinderen_MentorId",
                table: "Kinderen");

            migrationBuilder.DropColumn(
                name: "Beschikbaarheidsdagen",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "VasteStamgroepId",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "MentorId",
                table: "Kinderen");

            migrationBuilder.DropColumn(
                name: "MentorMedewerkerId",
                table: "Kinderen");
        }
    }
}
