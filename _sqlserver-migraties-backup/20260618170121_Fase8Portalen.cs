using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase8Portalen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Urenregistraties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedewerkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoosterdienstId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StamgroepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Ingeklokt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Uitgeklokt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.InsertData(
                table: "Capabilities",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Omschrijving", "Sleutel" },
                values: new object[,]
                {
                    { new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thuis-portaal gebruiken (eigen rooster, beschikbaarheid, verlof)", "MagThuisportaalGebruiken" },
                    { new Guid("d964b02e-1fb6-4050-a044-13488445cf14"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Groepsportaal op locatie gebruiken (inklokken, dienst, observaties)", "MagGroepsportaalGebruiken" }
                });

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[,]
                {
                    { new Guid("1a51ec29-a7a0-a0c9-fd46-8916cda2b2e4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d964b02e-1fb6-4050-a044-13488445cf14"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 },
                    { new Guid("208206c0-1691-8ede-fc6b-82df6f71fd83"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 3 },
                    { new Guid("4e41907a-bedf-f19f-78b6-cb3ef1247215"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 },
                    { new Guid("6a45542c-1ee8-400e-823e-cbbb3db4a8c0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("b8faed65-f228-2629-8cd8-61b654fde6cf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 }
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Urenregistraties");

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("1a51ec29-a7a0-a0c9-fd46-8916cda2b2e4"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("208206c0-1691-8ede-fc6b-82df6f71fd83"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("4e41907a-bedf-f19f-78b6-cb3ef1247215"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("6a45542c-1ee8-400e-823e-cbbb3db4a8c0"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("b8faed65-f228-2629-8cd8-61b654fde6cf"));

            migrationBuilder.DeleteData(
                table: "Capabilities",
                keyColumn: "Id",
                keyValue: new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"));

            migrationBuilder.DeleteData(
                table: "Capabilities",
                keyColumn: "Id",
                keyValue: new Guid("d964b02e-1fb6-4050-a044-13488445cf14"));
        }
    }
}
