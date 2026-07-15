using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StagiairEnBkrVlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TeltMeeVoorBkr",
                table: "Medewerkers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[] { new Guid("d84bb9c3-1057-7083-6042-468c7ceb6e10"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("c8b888f7-520c-8350-c7d2-f5723eb7517a"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("d84bb9c3-1057-7083-6042-468c7ceb6e10"));

            migrationBuilder.DropColumn(
                name: "TeltMeeVoorBkr",
                table: "Medewerkers");
        }
    }
}
