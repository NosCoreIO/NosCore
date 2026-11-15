using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261115001707_RemoveGameDataImport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GameDataImport");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GameDataImport",
            columns: table => new
            {
                GameDataImportId = table.Column<int>(type: "integer", nullable: false),
                ClientFiles = table.Column<string>(type: "text", nullable: false),
                Folder = table.Column<string>(type: "text", nullable: false),
                ParserBuild = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameDataImport", x => x.GameDataImportId);
            });
    }
}
