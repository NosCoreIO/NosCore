using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261114075552_AddSpeedBoostBuffs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoostDodgeChance",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<short>(
            name: "SpeedBoostEndCardId",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<byte>(
            name: "SpeedBoostRestoresHpPerLevel",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<short>(
            name: "SpeedBoostStartCardId",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SpeedBoostDodgeChance",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostEndCardId",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostRestoresHpPerLevel",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "SpeedBoostStartCardId",
            table: "Item");
    }
}
