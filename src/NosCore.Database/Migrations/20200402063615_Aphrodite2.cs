using System;
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

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentScriptId",
                table: "Character",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Script",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ScriptId = table.Column<byte>(nullable: false),
                    ScriptStepId = table.Column<short>(nullable: false),
                    StepType = table.Column<string>(nullable: false),
                    StringArgument = table.Column<string>(nullable: true),
                    Argument1 = table.Column<short>(nullable: true),
                    Argument2 = table.Column<short>(nullable: true),
                    Argument3 = table.Column<short>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Script", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Character_CurrentScriptId",
                table: "Character",
                column: "CurrentScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_Script_ScriptId_ScriptStepId",
                table: "Script",
                columns: new[] { "ScriptId", "ScriptStepId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Character_Script_CurrentScriptId",
                table: "Character",
                column: "CurrentScriptId",
                principalTable: "Script",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Character_Script_CurrentScriptId",
                table: "Character");

            migrationBuilder.DropTable(
                name: "Script");

            migrationBuilder.DropIndex(
                name: "IX_Character_CurrentScriptId",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "CurrentScriptId",
                table: "Character");

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
