using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Observaties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MentorMedewerkerId",
                table: "Kinderen");

            migrationBuilder.CreateTable(
                name: "Observaties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KindId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MijlpaalMaanden = table.Column<int>(type: "int", nullable: false),
                    BestandsNaam = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    BestandsSleutel = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BestandsGrootte = table.Column<long>(type: "bigint", nullable: false),
                    VerzondenOp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerzondenNaarEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AangemaaktOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GewijzigdOp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganisatieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observaties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observaties_Kinderen_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinderen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observaties_KindId_MijlpaalMaanden",
                table: "Observaties",
                columns: new[] { "KindId", "MijlpaalMaanden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observaties");

            migrationBuilder.AddColumn<Guid>(
                name: "MentorMedewerkerId",
                table: "Kinderen",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
