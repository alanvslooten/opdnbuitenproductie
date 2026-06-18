using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9Meldingen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Meldingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Soort = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VereistActie = table.Column<bool>(type: "bit", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tekst = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BronType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BronId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeduplicatieSleutel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AfgehandeldOp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meldingen", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Capabilities",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Omschrijving", "Sleutel" },
                values: new object[] { new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dashboard en het actiecentrum (meldingen/to-do's) inzien", "MagDashboardZien" });

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[,]
                {
                    { new Guid("8e76cde6-0e4d-0ba1-f541-d9a82152ea89"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 0 },
                    { new Guid("f9953744-0012-bc39-80e1-d0df5dd77c2f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 1 },
                    { new Guid("ff2fea9e-9489-1dd6-74cb-c2e447f8ad92"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meldingen_OrganisatieId_DeduplicatieSleutel_Status",
                table: "Meldingen",
                columns: new[] { "OrganisatieId", "DeduplicatieSleutel", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Meldingen_OrganisatieId_Status",
                table: "Meldingen",
                columns: new[] { "OrganisatieId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meldingen");

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("8e76cde6-0e4d-0ba1-f541-d9a82152ea89"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("f9953744-0012-bc39-80e1-d0df5dd77c2f"));

            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("ff2fea9e-9489-1dd6-74cb-c2e447f8ad92"));

            migrationBuilder.DeleteData(
                table: "Capabilities",
                keyColumn: "Id",
                keyValue: new Guid("d7b7e8f0-c6e1-61b3-0a88-0ecf6242600b"));
        }
    }
}
