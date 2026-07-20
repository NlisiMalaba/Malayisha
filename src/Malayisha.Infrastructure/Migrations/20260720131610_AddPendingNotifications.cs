using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Malayisha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_pending_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_pending_notifications_next_retry",
                table: "pending_notifications",
                column: "next_retry_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_pending_notifications_user_id",
                table: "pending_notifications",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_notifications");
        }
    }
}
