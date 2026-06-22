using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactenModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Wachtlijstinschrijvingen",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Kinderen",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Contacten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Voornaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Achternaam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefoon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsIntern = table.Column<bool>(type: "boolean", nullable: false),
                    Aantekeningen = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacten_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rondleidingen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notitie = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rondleidingen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rondleidingen_Contacten_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rondleidingen_Organisaties_OrganisatieId",
                        column: x => x.OrganisatieId,
                        principalTable: "Organisaties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wachtlijstinschrijvingen_ContactId",
                table: "Wachtlijstinschrijvingen",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Kinderen_ContactId",
                table: "Kinderen",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacten_OrganisatieId_Achternaam",
                table: "Contacten",
                columns: new[] { "OrganisatieId", "Achternaam" });

            migrationBuilder.CreateIndex(
                name: "IX_Rondleidingen_ContactId",
                table: "Rondleidingen",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Rondleidingen_OrganisatieId",
                table: "Rondleidingen",
                column: "OrganisatieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kinderen_Contacten_ContactId",
                table: "Kinderen",
                column: "ContactId",
                principalTable: "Contacten",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Wachtlijstinschrijvingen_Contacten_ContactId",
                table: "Wachtlijstinschrijvingen",
                column: "ContactId",
                principalTable: "Contacten",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kinderen_Contacten_ContactId",
                table: "Kinderen");

            migrationBuilder.DropForeignKey(
                name: "FK_Wachtlijstinschrijvingen_Contacten_ContactId",
                table: "Wachtlijstinschrijvingen");

            migrationBuilder.DropTable(
                name: "Rondleidingen");

            migrationBuilder.DropTable(
                name: "Contacten");

            migrationBuilder.DropIndex(
                name: "IX_Wachtlijstinschrijvingen_ContactId",
                table: "Wachtlijstinschrijvingen");

            migrationBuilder.DropIndex(
                name: "IX_Kinderen_ContactId",
                table: "Kinderen");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Wachtlijstinschrijvingen");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Kinderen");
        }
    }
}
