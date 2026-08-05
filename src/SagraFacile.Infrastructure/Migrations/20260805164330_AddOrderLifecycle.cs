using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagraFacile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Orders" SET "Status" = 'Delivered' WHERE "Status" = 'Completed';
                UPDATE "Orders" SET "Status" = 'CancelledByOperator' WHERE "Status" = 'Cancelled';
                UPDATE "Orders" SET "Status" = 'Confirmed' WHERE "Status" IN ('Preparing','Ready');
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'Delivered' WHERE "FromStatus" = 'Completed';
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'Delivered' WHERE "ToStatus"   = 'Completed';
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'CancelledByOperator' WHERE "FromStatus" = 'Cancelled';
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'CancelledByOperator' WHERE "ToStatus"   = 'Cancelled';
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'Confirmed' WHERE "FromStatus" IN ('Preparing','Ready');
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'Confirmed' WHERE "ToStatus"   IN ('Preparing','Ready');
                DELETE FROM "OrderStatusTransitions" WHERE "FromStatus" = "ToStatus";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "OrderStatusTransitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "OrderStatusTransitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "OrderStatusTransitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cashier");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "FulfilledAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOrderId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0,\"lifecycle\":{\"defaultConfirmerRole\":1,\"allowEditAfterConfirmation\":false,\"autoConfirmPreorders\":false,\"allowFollowUpOrders\":true,\"requireReasonOnRejection\":true,\"requireReasonOnCancellation\":false,\"transitionOverrides\":[]}}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0}}");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ParentOrderId",
                table: "Orders",
                column: "ParentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Orders_ParentOrderId",
                table: "Orders",
                column: "ParentOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Orders" SET "Status" = 'Completed' WHERE "Status" = 'Delivered';
                UPDATE "Orders" SET "Status" = 'Cancelled' WHERE "Status" IN ('CancelledByCustomer','CancelledByOperator');
                UPDATE "Orders" SET "Status" = 'Confirmed' WHERE "Status" IN ('Preorder','Rejected','Fulfilled');
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'Completed' WHERE "FromStatus" = 'Delivered';
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'Completed' WHERE "ToStatus"   = 'Delivered';
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'Cancelled' WHERE "FromStatus" IN ('CancelledByCustomer','CancelledByOperator');
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'Cancelled' WHERE "ToStatus"   IN ('CancelledByCustomer','CancelledByOperator');
                UPDATE "OrderStatusTransitions" SET "FromStatus" = 'Confirmed' WHERE "FromStatus" IN ('Preorder','Rejected','Fulfilled');
                UPDATE "OrderStatusTransitions" SET "ToStatus"   = 'Confirmed' WHERE "ToStatus"   IN ('Preorder','Rejected','Fulfilled');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Orders_ParentOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ParentOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "OrderStatusTransitions");

            migrationBuilder.DropColumn(
                name: "FulfilledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ParentOrderId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "OrderStatusTransitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "OrderStatusTransitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalOptions",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0}}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"reservations\":{\"partyCompletion\":{\"enabled\":false,\"minPartySize\":8}},\"view\":{\"showNotesField\":false,\"counterPeopleFirst\":true,\"showCallCount\":false,\"maxWaitTimeMinutes\":45},\"orders\":{\"enabledContexts\":[0,1,2],\"coverChargeEnabled\":false,\"defaultCoverChargeInCents\":0,\"lifecycle\":{\"defaultConfirmerRole\":1,\"allowEditAfterConfirmation\":false,\"autoConfirmPreorders\":false,\"allowFollowUpOrders\":true,\"requireReasonOnRejection\":true,\"requireReasonOnCancellation\":false,\"transitionOverrides\":[]}}}");
        }
    }
}
