using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAdditionalOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalOptions",
                table: "Events");
        }
    }
}
