using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePriorityWithNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TableReservations");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TableReservations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TableReservations");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TableReservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
