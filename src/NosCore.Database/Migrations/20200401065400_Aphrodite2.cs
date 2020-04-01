using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FifthObjective",
                table: "CharacterQuest");

            migrationBuilder.DropColumn(
                name: "FirstObjective",
                table: "CharacterQuest");

            migrationBuilder.DropColumn(
                name: "FourthObjective",
                table: "CharacterQuest");

            migrationBuilder.DropColumn(
                name: "IsMainQuest",
                table: "CharacterQuest");

            migrationBuilder.DropColumn(
                name: "SecondObjective",
                table: "CharacterQuest");

            migrationBuilder.DropColumn(
                name: "ThirdObjective",
                table: "CharacterQuest");

            migrationBuilder.AlterColumn<short>(
                name: "QuestType",
                table: "Quest",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "QuestType",
                table: "Quest",
                type: "integer",
                nullable: false,
                oldClrType: typeof(short));

            migrationBuilder.AddColumn<int>(
                name: "FifthObjective",
                table: "CharacterQuest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FirstObjective",
                table: "CharacterQuest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FourthObjective",
                table: "CharacterQuest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsMainQuest",
                table: "CharacterQuest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SecondObjective",
                table: "CharacterQuest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThirdObjective",
                table: "CharacterQuest",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
