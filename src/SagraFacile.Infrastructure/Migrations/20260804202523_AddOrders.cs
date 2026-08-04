using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45}}");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Context = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContextReferenceId = table.Column<int>(type: "integer", nullable: true),
                    ContextLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Covers = table.Column<int>(type: "integer", nullable: false),
                    CoverChargeInCents = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    MenuItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPriceInCents = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusTransitions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId_Position",
                table: "OrderLines",
                columns: new[] { "OrderId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Context_ContextReferenceId",
                table: "Orders",
                columns: new[] { "Context", "ContextReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EventId_OrderNumber",
                table: "Orders",
                columns: new[] { "EventId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EventId_Status",
                table: "Orders",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusTransitions_OrderId_OccurredAt",
                table: "OrderStatusTransitions",
                columns: new[] { "OrderId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "OrderStatusTransitions");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0}}");
        }
    }
}
