using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KennisbankBijlagen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KennisbankBijlagen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KennisbankDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BestandsNaam = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    BestandsSleutel = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BestandsGrootte = table.Column<long>(type: "bigint", nullable: false),
                    AangemaaktOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KennisbankBijlagen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KennisbankBijlagen_KennisbankDocumenten_KennisbankDocumentId",
                        column: x => x.KennisbankDocumentId,
                        principalTable: "KennisbankDocumenten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KennisbankBijlagen_KennisbankDocumentId",
                table: "KennisbankBijlagen",
                column: "KennisbankDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KennisbankBijlagen");
        }
    }
}
