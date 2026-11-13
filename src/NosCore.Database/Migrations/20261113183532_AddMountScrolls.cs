using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261113183532_AddMountScrolls : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ConsumedOnMount",
            table: "Item",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<byte>(
            name: "LevelMaximum",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConsumedOnMount",
            table: "Item");

        migrationBuilder.DropColumn(
            name: "LevelMaximum",
            table: "Item");
    }
}
