using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DienstsoortEnKinderenLezen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "Medewerkers");

            migrationBuilder.AddColumn<int>(
                name: "Dienstsoort",
                table: "Roosterdiensten",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Capabilities",
                columns: new[] { "Id", "AangemaaktOp", "GewijzigdOp", "Omschrijving", "Sleutel" },
                values: new object[] { new Guid("6ec3065d-93e6-e8a0-c8c4-169f71b3f97e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kindgegevens inzien (alleen-lezen)", "MagKinderenLezen" });

            migrationBuilder.InsertData(
                table: "RolCapabilities",
                columns: new[] { "Id", "AangemaaktOp", "CapabilityId", "GewijzigdOp", "OrganisatieId", "Rol" },
                values: new object[] { new Guid("9154046e-d12f-5e3d-549d-1258214616d6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("6ec3065d-93e6-e8a0-c8c4-169f71b3f97e"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("0a000000-0000-0000-0000-000000000001"), 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolCapabilities",
                keyColumn: "Id",
                keyValue: new Guid("9154046e-d12f-5e3d-549d-1258214616d6"));

            migrationBuilder.DeleteData(
                table: "Capabilities",
                keyColumn: "Id",
                keyValue: new Guid("6ec3065d-93e6-e8a0-c8c4-169f71b3f97e"));

            migrationBuilder.DropColumn(
                name: "Dienstsoort",
                table: "Roosterdiensten");

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "Medewerkers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
