using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261120181952_UniqueQuestObjectivesAndPrizes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_QuestQuestReward_QuestId",
            table: "QuestQuestReward");

        migrationBuilder.DropIndex(
            name: "IX_QuestObjective_QuestId",
            table: "QuestObjective");

        // Earlier imports wrote each objective and prize several times; keep one, with the best progress on it.
        migrationBuilder.Sql(@"
            CREATE TEMP TABLE ""QuestObjectiveCopy"" AS
            SELECT ""QuestObjectiveId"" AS ""CopyId"", ""KeptId"" FROM (
                SELECT ""QuestObjectiveId"", first_value(""QuestObjectiveId"") OVER (
                    PARTITION BY ""QuestId"", ""FirstData"", ""SecondData"", ""ThirdData"", ""FourthData"" ORDER BY ""QuestObjectiveId"") AS ""KeptId""
                FROM ""QuestObjective"") o
            WHERE ""QuestObjectiveId"" <> ""KeptId"";

            DELETE FROM ""CharacterQuestObjective"" p USING (
                SELECT p.""Id"", row_number() OVER (
                    PARTITION BY p.""CharacterQuestId"", coalesce(c.""KeptId"", p.""QuestObjectiveId"") ORDER BY p.""Count"" DESC, p.""Id"") AS ""Rank""
                FROM ""CharacterQuestObjective"" p LEFT JOIN ""QuestObjectiveCopy"" c ON c.""CopyId"" = p.""QuestObjectiveId"") r
            WHERE p.""Id"" = r.""Id"" AND r.""Rank"" > 1;

            UPDATE ""CharacterQuestObjective"" p SET ""QuestObjectiveId"" = c.""KeptId""
            FROM ""QuestObjectiveCopy"" c WHERE p.""QuestObjectiveId"" = c.""CopyId"";

            DELETE FROM ""QuestObjective"" o USING ""QuestObjectiveCopy"" c WHERE o.""QuestObjectiveId"" = c.""CopyId"";
            DROP TABLE ""QuestObjectiveCopy"";

            DELETE FROM ""QuestQuestReward"" l USING (
                SELECT ""Id"", row_number() OVER (PARTITION BY ""QuestId"", ""QuestRewardId"" ORDER BY ""Id"") AS ""Rank""
                FROM ""QuestQuestReward"") r
            WHERE l.""Id"" = r.""Id"" AND r.""Rank"" > 1;
            ");

        migrationBuilder.CreateIndex(
            name: "IX_QuestQuestReward_QuestId_QuestRewardId",
            table: "QuestQuestReward",
            columns: new[] { "QuestId", "QuestRewardId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_QuestObjective_QuestId_FirstData_SecondData_ThirdData_Fourt~",
            table: "QuestObjective",
            columns: new[] { "QuestId", "FirstData", "SecondData", "ThirdData", "FourthData" },
            unique: true)
            .Annotation("Npgsql:NullsDistinct", false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_QuestQuestReward_QuestId_QuestRewardId",
            table: "QuestQuestReward");

        migrationBuilder.DropIndex(
            name: "IX_QuestObjective_QuestId_FirstData_SecondData_ThirdData_Fourt~",
            table: "QuestObjective");

        migrationBuilder.CreateIndex(
            name: "IX_QuestQuestReward_QuestId",
            table: "QuestQuestReward",
            column: "QuestId");

        migrationBuilder.CreateIndex(
            name: "IX_QuestObjective_QuestId",
            table: "QuestObjective",
            column: "QuestId");
    }
}
