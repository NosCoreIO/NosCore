using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261115032624_AddMonsterChatter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "ChatterGroup",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.CreateTable(
            name: "ChatterLine",
            columns: table => new
            {
                ChatterLineId = table.Column<short>(type: "smallint", nullable: false),
                ChatterGroup = table.Column<byte>(type: "smallint", nullable: false),
                Message = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatterLine", x => x.ChatterLineId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ChatterLine");

        migrationBuilder.DropColumn(
            name: "ChatterGroup",
            table: "NpcMonster");
    }
}
