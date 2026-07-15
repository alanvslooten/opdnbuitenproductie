using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactLogboek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactLogregels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Omschrijving = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactLogregels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactLogregels_Contacten_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactLogregels_ContactId",
                table: "ContactLogregels",
                column: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactLogregels");
        }
    }
}
