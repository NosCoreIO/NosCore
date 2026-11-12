using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261112083755_AddMountBoostEffects : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoostRemovesDebuffsUpToLevel",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoostRestoresPerLevel",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<bool>(
            name: "SpeedBoostTeleportsRandomly",
            table: "Item",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SpeedBoostRemovesDebuffsUpToLevel",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostRestoresPerLevel",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostTeleportsRandomly",
            table: "Item");
    }
}
