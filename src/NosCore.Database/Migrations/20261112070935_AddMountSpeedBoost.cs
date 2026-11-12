using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261112070935_AddMountSpeedBoost : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoost",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoostDuration",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SpeedBoost",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostDuration",
            table: "Item");
    }
}
