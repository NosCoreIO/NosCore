using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewAuthPassword",
                table: "Account",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewAuthSalt",
                table: "Account",
                maxLength: 255,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewAuthPassword",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "NewAuthSalt",
                table: "Account");
        }
    }
}
