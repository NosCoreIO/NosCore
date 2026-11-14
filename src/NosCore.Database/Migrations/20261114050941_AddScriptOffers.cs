using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261114050941_AddScriptOffers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ScriptOffer",
            columns: table => new
            {
                ScriptId = table.Column<byte>(type: "smallint", nullable: false),
                NpcVNum = table.Column<short>(type: "smallint", nullable: false),
                RequiredScriptId = table.Column<byte>(type: "smallint", nullable: false),
                LevelMinimum = table.Column<byte>(type: "smallint", nullable: false),
                DialogId = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScriptOffer", x => x.ScriptId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ScriptOffer");
    }
}
