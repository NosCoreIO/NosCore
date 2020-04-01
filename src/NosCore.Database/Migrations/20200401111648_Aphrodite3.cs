using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Script",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ScriptId = table.Column<byte>(nullable: false),
                    ScriptStepId = table.Column<short>(nullable: false),
                    StepType = table.Column<string>(nullable: false),
                    Argument = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Script", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Script_ScriptId_ScriptStepId",
                table: "Script",
                columns: new[] { "ScriptId", "ScriptStepId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Script");
        }
    }
}
