using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261112051510_AddNpcMonsterStatBonuses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "CloseDefenceBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "ConcentrateBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "DamageMaximumBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "DamageMinimumBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "DistanceDefenceBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "DodgeBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<short>(
            name: "MagicDefenceBonus",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CloseDefenceBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "ConcentrateBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "DamageMaximumBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "DamageMinimumBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "DistanceDefenceBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "DodgeBonus",
            table: "NpcMonster");

        migrationBuilder.DropColumn(
            name: "MagicDefenceBonus",
            table: "NpcMonster");
    }
}
