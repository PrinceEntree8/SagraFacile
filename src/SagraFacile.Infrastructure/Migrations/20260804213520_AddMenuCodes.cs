using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "MenuItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "MenuCategories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""MenuCategories"" SET ""Code"" = lower(regexp_replace(""Name"", '[^a-zA-Z0-9]+', '-', 'g')) || '-' || ""Id"" WHERE ""Code"" = '';
                UPDATE ""MenuItems"" SET ""Code"" = lower(regexp_replace(""Name"", '[^a-zA-Z0-9]+', '-', 'g')) || '-' || ""Id"" WHERE ""Code"" = '';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_EventId_Code",
                table: "MenuItems",
                columns: new[] { "EventId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_Code",
                table: "MenuCategories",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_EventId_Code",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_Code",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "MenuCategories");
        }
    }
}
