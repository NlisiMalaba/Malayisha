using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Malayisha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripListingBoostFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_trip_search",
                table: "trip_listings");

            migrationBuilder.AddColumn<DateTime>(
                name: "boost_end_at_utc",
                table: "trip_listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "boost_start_at_utc",
                table: "trip_listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_boosted",
                table: "trip_listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "idx_trip_search",
                table: "trip_listings",
                columns: new[] { "origin_city", "destination_city", "departure_date_utc", "is_boosted" },
                descending: new[] { false, false, false, true },
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_trip_search",
                table: "trip_listings");

            migrationBuilder.DropColumn(
                name: "boost_end_at_utc",
                table: "trip_listings");

            migrationBuilder.DropColumn(
                name: "boost_start_at_utc",
                table: "trip_listings");

            migrationBuilder.DropColumn(
                name: "is_boosted",
                table: "trip_listings");

            migrationBuilder.CreateIndex(
                name: "idx_trip_search",
                table: "trip_listings",
                columns: new[] { "origin_city", "destination_city", "departure_date_utc" },
                filter: "is_deleted = false");
        }
    }
}
