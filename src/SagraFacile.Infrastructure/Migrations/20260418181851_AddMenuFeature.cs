using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Allergens");

            migrationBuilder.DropColumn(
                name: "NameIt",
                table: "Allergens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "MenuCategories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Allergens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameIt",
                table: "Allergens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
