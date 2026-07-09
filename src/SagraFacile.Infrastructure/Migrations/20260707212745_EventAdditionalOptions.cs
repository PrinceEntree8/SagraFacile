using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventAdditionalOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45}}");
        }
    }
}
