using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Language",
                table: "Account",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Account_Name",
                table: "Account",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Account_Name",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Account");
        }
    }
}
