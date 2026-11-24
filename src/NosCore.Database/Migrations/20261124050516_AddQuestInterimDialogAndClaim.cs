using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261124050516_AddQuestInterimDialogAndClaim : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InterimDialogId",
            table: "Quest",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<short>(
            name: "InterimNpcVNum",
            table: "Quest",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<Instant>(
            name: "ObjectivesCompletedOn",
            table: "CharacterQuest",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InterimDialogId",
            table: "Quest");

        migrationBuilder.DropColumn(
            name: "InterimNpcVNum",
            table: "Quest");

        migrationBuilder.DropColumn(
            name: "ObjectivesCompletedOn",
            table: "CharacterQuest");
    }
}
