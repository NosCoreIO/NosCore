using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261122120647_AddScriptTeleport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "TeleportMapId",
            table: "Script",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<short>(
            name: "TeleportX",
            table: "Script",
            type: "smallint",
            nullable: true);

        migrationBuilder.AddColumn<short>(
            name: "TeleportY",
            table: "Script",
            type: "smallint",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TeleportMapId",
            table: "Script");

        migrationBuilder.DropColumn(
            name: "TeleportX",
            table: "Script");

        migrationBuilder.DropColumn(
            name: "TeleportY",
            table: "Script");
    }
}
