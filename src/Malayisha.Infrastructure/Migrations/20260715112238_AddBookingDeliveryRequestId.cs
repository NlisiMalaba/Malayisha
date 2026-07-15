using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Malayisha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDeliveryRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "delivery_request_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_delivery_request_id",
                table: "bookings",
                column: "delivery_request_id",
                unique: true,
                filter: "delivery_request_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_delivery_requests_delivery_request_id",
                table: "bookings",
                column: "delivery_request_id",
                principalTable: "delivery_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_delivery_requests_delivery_request_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_delivery_request_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "delivery_request_id",
                table: "bookings");
        }
    }
}
