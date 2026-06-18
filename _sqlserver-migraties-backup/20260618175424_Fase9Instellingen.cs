using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase9Instellingen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganisatieInstellingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerborgenMeldingsoorten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObservatieBinnenkortDrempelDagen = table.Column<int>(type: "int", nullable: false),
                    KindBinnenkortVierDrempelDagen = table.Column<int>(type: "int", nullable: false),
                    StandaardObservatietekst = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PrioriteitInternGewicht = table.Column<int>(type: "int", nullable: false),
                    PrioriteitPerMaandGewicht = table.Column<int>(type: "int", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisatieInstellingen", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OrganisatieInstellingen",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "KindBinnenkortVierDrempelDagen", "ObservatieBinnenkortDrempelDagen", "OrganisatieId", "PrioriteitInternGewicht", "PrioriteitPerMaandGewicht", "StandaardObservatietekst", "VerborgenMeldingsoorten" },
                values: new object[] { new Guid("0c000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 90, 30, new Guid("0a000000-0000-0000-0000-000000000001"), 500, 10, null, "" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisatieInstellingen_OrganisatieId",
                table: "OrganisatieInstellingen",
                column: "OrganisatieId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganisatieInstellingen");
        }
    }
}
