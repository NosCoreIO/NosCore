using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterQuestObjective : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterQuestObjective",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterQuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterQuestObjective", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterQuestObjective_CharacterQuest_CharacterQuestId",
                        column: x => x.CharacterQuestId,
                        principalTable: "CharacterQuest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterQuestObjective_QuestObjective_QuestObjectiveId",
                        column: x => x.QuestObjectiveId,
                        principalTable: "QuestObjective",
                        principalColumn: "QuestObjectiveId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestObjective_CharacterQuestId_QuestObjectiveId",
                table: "CharacterQuestObjective",
                columns: new[] { "CharacterQuestId", "QuestObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuestObjective_QuestObjectiveId",
                table: "CharacterQuestObjective",
                column: "QuestObjectiveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterQuestObjective");
        }
    }
}
