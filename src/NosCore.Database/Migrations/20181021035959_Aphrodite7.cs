using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "I18NBCardId",
                table: "I18NBCard",
                newName: "I18NbCardId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "I18NbCardId",
                table: "I18NBCard",
                newName: "I18NBCardId");
        }
    }
}
