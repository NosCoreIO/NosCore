using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class ParseExtraNpcMonsterColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "BasicHitChance",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "CanOnlyBeDmgedByJajamaruLastSkill",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "CellSize",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "DashSpeed",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<short>(
                name: "EffectIdConstantly",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "EffectIdOnDeath",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte>(
                name: "GroupAttack",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<short>(
                name: "IconId",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPercentileDmg",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsValhallaPartner",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PetInfoVal1",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PetInfoVal2",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PetInfoVal3",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PetInfoVal4",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpawnMobOrColor",
                table: "NpcMonster",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "SpriteSize",
                table: "NpcMonster",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleOnMinimapAsGreenDot",
                table: "NpcMonster",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasicHitChance",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CanOnlyBeDmgedByJajamaruLastSkill",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "CellSize",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "DashSpeed",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "EffectIdConstantly",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "EffectIdOnDeath",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "GroupAttack",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "IsPercentileDmg",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "IsValhallaPartner",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "PetInfoVal1",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "PetInfoVal2",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "PetInfoVal3",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "PetInfoVal4",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "SpawnMobOrColor",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "SpriteSize",
                table: "NpcMonster");

            migrationBuilder.DropColumn(
                name: "VisibleOnMinimapAsGreenDot",
                table: "NpcMonster");
        }
    }
}
