using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedewerkerDetailsEnUrencorrectie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GecorrigeerdDoorUserId",
                table: "Urenregistraties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GecorrigeerdOp",
                table: "Urenregistraties",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractVast",
                table: "Medewerkers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Contractbegindatum",
                table: "Medewerkers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Contracteinddatum",
                table: "Medewerkers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Medewerkers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoodcontactNaam",
                table: "Medewerkers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoodcontactTelefoon",
                table: "Medewerkers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "Medewerkers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefoon",
                table: "Medewerkers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GecorrigeerdDoorUserId",
                table: "Urenregistraties");

            migrationBuilder.DropColumn(
                name: "GecorrigeerdOp",
                table: "Urenregistraties");

            migrationBuilder.DropColumn(
                name: "ContractVast",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "Contractbegindatum",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "Contracteinddatum",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "NoodcontactNaam",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "NoodcontactTelefoon",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "Medewerkers");

            migrationBuilder.DropColumn(
                name: "Telefoon",
                table: "Medewerkers");
        }
    }
}
