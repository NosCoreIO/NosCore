using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EqPacket",
                table: "Mail");

            migrationBuilder.AddColumn<short>(
                name: "Armor",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "CostumeHat",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "CostumeSuit",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Fairy",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Hat",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "MainWeapon",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Mask",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SecondaryWeapon",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "WeaponSkin",
                table: "Mail",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "WingSkin",
                table: "Mail",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Armor",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "CostumeHat",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "CostumeSuit",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "Fairy",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "Hat",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "MainWeapon",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "Mask",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "SecondaryWeapon",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "WeaponSkin",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "WingSkin",
                table: "Mail");

            migrationBuilder.AddColumn<string>(
                name: "EqPacket",
                table: "Mail",
                maxLength: 255,
                nullable: true);
        }
    }
}
