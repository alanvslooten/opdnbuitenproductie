using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KennisbankToewijzing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lege array-default zodat de niet-nullable kolom ook op eventueel bestaande
            // documenten toepasbaar is (leeg = voor iedereen zichtbaar).
            migrationBuilder.AddColumn<List<Guid>>(
                name: "ToegewezenMedewerkerIds",
                table: "KennisbankDocumenten",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToegewezenMedewerkerIds",
                table: "KennisbankDocumenten");
        }
    }
}
