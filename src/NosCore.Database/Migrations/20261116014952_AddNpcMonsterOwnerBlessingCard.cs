using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261116014952_AddNpcMonsterOwnerBlessingCard : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "OwnerBlessingCardId",
            table: "NpcMonster",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OwnerBlessingCardId",
            table: "NpcMonster");
    }
}
