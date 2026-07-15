using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Diensttijden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Oudercontact_Rol",
                table: "Wachtlijstinschrijvingen",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Begintijd",
                table: "Roosterdiensten",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Eindtijd",
                table: "Roosterdiensten",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Oudercontact_Rol",
                table: "Wachtlijstinschrijvingen");

            migrationBuilder.DropColumn(
                name: "Begintijd",
                table: "Roosterdiensten");

            migrationBuilder.DropColumn(
                name: "Eindtijd",
                table: "Roosterdiensten");
        }
    }
}
