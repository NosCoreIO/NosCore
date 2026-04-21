using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class ParseMonsterDatExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<byte>(
                name: "Force",
                table: "NpcMonsterSkill",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "AlwaysActive",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "CModeVNum",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "CanCatch",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanCollect",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CantDebuff",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CantTargetInfo",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CantVoke",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "CellMinRange",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "DisappearAfterHitting",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DisappearAfterSeconds",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DisappearAfterSecondsMana",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DontDrainHpAfterSeconds",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDash",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMode",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "HpThreshold",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte>(
                name: "Limiter",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "Midgard",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OnDefenseOnlyOnce",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "RangeThreshold",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "RegenerateHpOverTime",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Force",
                table: "NpcMonsterSkill");

            migrationBuilder.DropColumn(
                name: "AlwaysActive",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CModeVNum",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CanCatch",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CanCollect",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CantDebuff",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CantTargetInfo",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CantVoke",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CellMinRange",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "DisappearAfterHitting",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "DisappearAfterSeconds",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "DisappearAfterSecondsMana",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "DontDrainHpAfterSeconds",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "HasDash",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "HasMode",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "HpThreshold",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "Limiter",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "Midgard",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "OnDefenseOnlyOnce",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "RangeThreshold",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "RegenerateHpOverTime",
                table: "NpcMonster");

        }
    }
}
