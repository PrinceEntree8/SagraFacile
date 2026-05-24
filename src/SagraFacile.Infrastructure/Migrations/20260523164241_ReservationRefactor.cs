using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReservationRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationCalls_TableReservations_TableReservationId",
                table: "ReservationCalls");

            migrationBuilder.DropTable(
                name: "TableReservations");

            migrationBuilder.RenameColumn(
                name: "TableReservationId",
                table: "ReservationCalls",
                newName: "ReservationId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationCalls_TableReservationId",
                table: "ReservationCalls",
                newName: "IX_ReservationCalls_ReservationId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ReservationCalls",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstCalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SeatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CallCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_EventId_SequenceNumber",
                table: "Reservations",
                columns: new[] { "EventId", "SequenceNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationCalls_Reservations_ReservationId",
                table: "ReservationCalls",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationCalls_Reservations_ReservationId",
                table: "ReservationCalls");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "ReservationCalls",
                newName: "TableReservationId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationCalls_ReservationId",
                table: "ReservationCalls",
                newName: "IX_ReservationCalls_TableReservationId");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "ReservationCalls",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TableReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CallCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FirstCalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    QueueNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SeatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableReservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_QueueNumber",
                table: "TableReservations",
                column: "QueueNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationCalls_TableReservations_TableReservationId",
                table: "ReservationCalls",
                column: "TableReservationId",
                principalTable: "TableReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
