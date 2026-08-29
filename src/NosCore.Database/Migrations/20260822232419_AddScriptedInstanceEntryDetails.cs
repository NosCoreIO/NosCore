using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptedInstanceEntryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHeroic",
                table: "ScriptedInstance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "LevelMaximum",
                table: "ScriptedInstance",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "LevelMinimum",
                table: "ScriptedInstance",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeroic",
                table: "ScriptedInstance");

            migrationBuilder.DropColumn(
                name: "LevelMaximum",
                table: "ScriptedInstance");

            migrationBuilder.DropColumn(
                name: "LevelMinimum",
                table: "ScriptedInstance");
        }
    }
}
