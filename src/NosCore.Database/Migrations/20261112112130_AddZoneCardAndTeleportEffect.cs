using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261112112130_AddZoneCardAndTeleportEffect : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "ZoneCardId",
            table: "MapType",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<short>(
            name: "SpeedBoostTeleportEffect",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ZoneCardId",
            table: "MapType");

        migrationBuilder.DropColumn(
            name: "SpeedBoostTeleportEffect",
            table: "Item");
    }
}
