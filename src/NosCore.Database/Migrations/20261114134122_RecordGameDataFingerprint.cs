using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261114134122_RecordGameDataFingerprint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // A row from before has no folder to import again from.
        migrationBuilder.Sql("DELETE FROM \"GameDataImport\";");

        migrationBuilder.DropColumn(
            name: "Version",
            table: "GameDataImport");

        migrationBuilder.AddColumn<string>(
            name: "ClientFiles",
            table: "GameDataImport",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Folder",
            table: "GameDataImport",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ParserBuild",
            table: "GameDataImport",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ClientFiles",
            table: "GameDataImport");

        migrationBuilder.DropColumn(
            name: "Folder",
            table: "GameDataImport");

        migrationBuilder.DropColumn(
            name: "ParserBuild",
            table: "GameDataImport");

        migrationBuilder.AddColumn<int>(
            name: "Version",
            table: "GameDataImport",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
