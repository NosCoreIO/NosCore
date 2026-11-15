using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261115131109_AddSpecialPotionEffects : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "RemovesDebuffsUpToLevel",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<byte>(
            name: "RestoresPercent",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RemovesDebuffsUpToLevel",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "RestoresPercent",
            table: "Item");
    }
}
