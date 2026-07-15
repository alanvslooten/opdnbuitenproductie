using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Dagplaatsing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dagplaatsingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    StamgroepId = table.Column<Guid>(type: "uuid", nullable: true),
                    Soort = table.Column<int>(type: "integer", nullable: false),
                    Notitie = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dagplaatsingen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dagplaatsingen_Kinderen_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinderen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Dagplaatsingen_Stamgroepen_StamgroepId",
                        column: x => x.StamgroepId,
                        principalTable: "Stamgroepen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dagplaatsingen_KindId_Datum",
                table: "Dagplaatsingen",
                columns: new[] { "KindId", "Datum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dagplaatsingen_OrganisatieId_Datum",
                table: "Dagplaatsingen",
                columns: new[] { "OrganisatieId", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_Dagplaatsingen_StamgroepId",
                table: "Dagplaatsingen",
                column: "StamgroepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dagplaatsingen");
        }
    }
}
