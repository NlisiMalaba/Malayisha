using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Malayisha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageIsDelivered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chat_messages_booking_id",
                table: "chat_messages");

            migrationBuilder.AddColumn<bool>(
                name: "is_delivered",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "idx_chat_messages_booking_undelivered",
                table: "chat_messages",
                columns: new[] { "booking_id", "is_delivered" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_chat_messages_booking_undelivered",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_delivered",
                table: "chat_messages");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_booking_id",
                table: "chat_messages",
                column: "booking_id");
        }
    }
}
