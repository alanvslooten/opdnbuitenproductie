using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Kennisbank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KennisbankDocumenten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Categorie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Inhoud = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KennisbankDocumenten", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KennisbankDocumenten_OrganisatieId_Categorie",
                table: "KennisbankDocumenten",
                columns: new[] { "OrganisatieId", "Categorie" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KennisbankDocumenten");
        }
    }
}
