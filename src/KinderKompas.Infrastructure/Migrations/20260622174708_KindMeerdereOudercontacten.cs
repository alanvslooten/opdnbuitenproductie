using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KinderKompas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KindMeerdereOudercontacten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Nieuwe jsonb-kolom toevoegen.
            migrationBuilder.AddColumn<string>(
                name: "Oudercontacten",
                table: "Kinderen",
                type: "jsonb",
                nullable: true);

            // 2) Bestaand enkel oudercontact naar een 1-elements JSON-array migreren
            //    (sleutels in PascalCase = de property-namen die EF in JSON verwacht).
            migrationBuilder.Sql(@"
                UPDATE ""Kinderen""
                SET ""Oudercontacten"" = jsonb_build_array(jsonb_build_object(
                    'Naam', ""Oudercontact_Naam"",
                    'Telefoon', COALESCE(""Oudercontact_Telefoon"", ''),
                    'Email', COALESCE(""Oudercontact_Email"", '')))
                WHERE ""Oudercontact_Naam"" IS NOT NULL AND ""Oudercontact_Naam"" <> '';");

            // 3) Oude kolommen verwijderen.
            migrationBuilder.DropColumn(name: "Oudercontact_Email", table: "Kinderen");
            migrationBuilder.DropColumn(name: "Oudercontact_Naam", table: "Kinderen");
            migrationBuilder.DropColumn(name: "Oudercontact_Telefoon", table: "Kinderen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Oudercontacten",
                table: "Kinderen");

            migrationBuilder.AddColumn<string>(
                name: "Oudercontact_Email",
                table: "Kinderen",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Oudercontact_Naam",
                table: "Kinderen",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Oudercontact_Telefoon",
                table: "Kinderen",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }
    }
}
