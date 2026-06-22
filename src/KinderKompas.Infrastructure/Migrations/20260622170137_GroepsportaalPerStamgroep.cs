using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GroepsportaalPerStamgroep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("c2dcaad8-24e7-5675-a3de-6e32ebf9c98b"));

            migrationBuilder.AddColumn<Guid>(
                name: "StamgroepId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StamgroepId",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[] { new Guid("c2dcaad8-24e7-5675-a3de-6e32ebf9c98b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("73b5d451-8c90-336e-a5c4-18449b7533d3"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 });
        }
    }
}
