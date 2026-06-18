using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "Organisaties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Naam = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Lrknummer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisaties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medewerkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Voornaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    VasteWerkdagen = table.Column<int>(type: "int", nullable: false),
                    Contracturen = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Stamgroepen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Naam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxKinderen = table.Column<int>(type: "int", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "Kinderen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Voornaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Geboortedatum = table.Column<DateOnly>(type: "date", nullable: false),
                    StamgroepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Startdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Einddatum = table.Column<DateOnly>(type: "date", nullable: true),
                    Contracttype = table.Column<int>(type: "int", nullable: false),
                    GewensteOpvangdagen = table.Column<int>(type: "int", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinderen", x => x.Id);
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

            migrationBuilder.InsertData(
                table: "Organisaties",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Lrknummer", "Naam" },
                values: new object[] { new Guid("0a000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "000000000", "Op d'n Buiten" });

            migrationBuilder.InsertData(
                table: "Stamgroepen",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "MaxKinderen", "Naam", "OrganisatieId" },
                values: new object[,]
                {
                    { new Guid("0b000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, "Bengeltjes", new Guid("0a000000-0000-0000-0000-000000000001") },
                    { new Guid("0b000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, "Boefjes", new Guid("0a000000-0000-0000-0000-000000000001") }
                });

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
                name: "IX_Stamgroepen_OrganisatieId",
                table: "Stamgroepen",
                column: "OrganisatieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kinderen");

            migrationBuilder.DropTable(
                name: "Medewerkers");

            migrationBuilder.DropTable(
                name: "Stamgroepen");

            migrationBuilder.DropTable(
                name: "Organisaties");
        }
    }
}
