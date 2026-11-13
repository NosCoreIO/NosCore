using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261113155018_AddInstantCombatWaves : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InstantCombatDrop",
            columns: table => new
            {
                InstantCombatDropId = table.Column<short>(type: "smallint", nullable: false),
                LevelMinimum = table.Column<byte>(type: "smallint", nullable: false),
                Wave = table.Column<byte>(type: "smallint", nullable: false),
                ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                Piles = table.Column<short>(type: "smallint", nullable: false),
                Amount = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstantCombatDrop", x => x.InstantCombatDropId);
            });

        migrationBuilder.CreateTable(
            name: "InstantCombatMonster",
            columns: table => new
            {
                InstantCombatMonsterId = table.Column<short>(type: "smallint", nullable: false),
                LevelMinimum = table.Column<byte>(type: "smallint", nullable: false),
                Wave = table.Column<byte>(type: "smallint", nullable: false),
                NpcMonsterVNum = table.Column<short>(type: "smallint", nullable: false),
                Amount = table.Column<short>(type: "smallint", nullable: false),
                IsMoving = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstantCombatMonster", x => x.InstantCombatMonsterId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InstantCombatDrop");

        migrationBuilder.DropTable(
            name: "InstantCombatMonster");
    }
}
