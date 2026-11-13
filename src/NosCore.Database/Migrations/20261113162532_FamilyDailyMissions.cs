using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261113162532_FamilyDailyMissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Instant>(
            name: "ProgressedAt",
            table: "FamilyQuestProgress",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "OncePerMemberPerDay",
            table: "FamilyQuest",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ResetsDaily",
            table: "FamilyQuest",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "FamilyQuestContribution",
            columns: table => new
            {
                FamilyQuestContributionId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FamilyId = table.Column<long>(type: "bigint", nullable: false),
                QuestId = table.Column<short>(type: "smallint", nullable: false),
                CharacterId = table.Column<long>(type: "bigint", nullable: false),
                ContributedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FamilyQuestContribution", x => x.FamilyQuestContributionId);
                table.ForeignKey(
                    name: "FK_FamilyQuestContribution_Family_FamilyId",
                    column: x => x.FamilyId,
                    principalTable: "Family",
                    principalColumn: "FamilyId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FamilyQuestContribution_FamilyId_QuestId_CharacterId",
            table: "FamilyQuestContribution",
            columns: new[] { "FamilyId", "QuestId", "CharacterId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FamilyQuestContribution");

        migrationBuilder.DropColumn(
            name: "ProgressedAt",
            table: "FamilyQuestProgress");

        migrationBuilder.DropColumn(
            name: "OncePerMemberPerDay",
            table: "FamilyQuest");

        migrationBuilder.DropColumn(
            name: "ResetsDaily",
            table: "FamilyQuest");
    }
}
