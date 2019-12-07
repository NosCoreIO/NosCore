using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InfoId",
                table: "Quest");

            migrationBuilder.AddColumn<string>(
                name: "Desc",
                table: "Quest",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Quest",
                maxLength: 255,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Desc",
                table: "Quest");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Quest");

            migrationBuilder.AddColumn<int>(
                name: "InfoId",
                table: "Quest",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
