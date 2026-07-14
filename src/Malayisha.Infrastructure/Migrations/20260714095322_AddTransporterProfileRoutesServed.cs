using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Malayisha.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTransporterProfileRoutesServed : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "routes_served",
            table: "transporter_profiles",
            type: "jsonb",
            nullable: false,
            defaultValueSql: "'[]'::jsonb");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "routes_served",
            table: "transporter_profiles");
    }
}
