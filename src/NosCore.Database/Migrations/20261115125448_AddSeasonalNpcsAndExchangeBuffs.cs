using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261115125448_AddSeasonalNpcsAndExchangeBuffs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "CardId",
            table: "NpcExchange",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "Season",
            table: "MapNpc",
            type: "smallint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_NpcExchange_CardId",
            table: "NpcExchange",
            column: "CardId");

        migrationBuilder.AddForeignKey(
            name: "FK_NpcExchange_Card_CardId",
            table: "NpcExchange",
            column: "CardId",
            principalTable: "Card",
            principalColumn: "CardId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NpcExchange_Card_CardId",
            table: "NpcExchange");

        migrationBuilder.DropIndex(
            name: "IX_NpcExchange_CardId",
            table: "NpcExchange");

        migrationBuilder.DropColumn(
            name: "CardId",
            table: "NpcExchange");

        migrationBuilder.DropColumn(
            name: "Season",
            table: "MapNpc");
    }
}
