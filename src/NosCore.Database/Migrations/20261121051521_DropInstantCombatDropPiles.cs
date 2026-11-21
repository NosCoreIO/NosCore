using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261121051521_DropInstantCombatDropPiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Piles",
            table: "InstantCombatDrop");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "Piles",
            table: "InstantCombatDrop",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);
    }
}
