using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Argument",
                table: "Script");

            migrationBuilder.AddColumn<short>(
                name: "Argument1",
                table: "Script",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Argument2",
                table: "Script",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Argument3",
                table: "Script",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StringArgument",
                table: "Script",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Argument1",
                table: "Script");

            migrationBuilder.DropColumn(
                name: "Argument2",
                table: "Script");

            migrationBuilder.DropColumn(
                name: "Argument3",
                table: "Script");

            migrationBuilder.DropColumn(
                name: "StringArgument",
                table: "Script");

            migrationBuilder.AddColumn<string>(
                name: "Argument",
                table: "Script",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
