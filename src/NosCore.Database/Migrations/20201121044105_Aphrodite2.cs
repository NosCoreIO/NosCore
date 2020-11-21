using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Q2",
                table: "QuicklistEntry",
                newName: "QuickListIndex");

            migrationBuilder.RenameColumn(
                name: "Q1",
                table: "QuicklistEntry",
                newName: "IconVNum");

            migrationBuilder.RenameColumn(
                name: "Pos",
                table: "QuicklistEntry",
                newName: "IconType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuickListIndex",
                table: "QuicklistEntry",
                newName: "Q2");

            migrationBuilder.RenameColumn(
                name: "IconVNum",
                table: "QuicklistEntry",
                newName: "Q1");

            migrationBuilder.RenameColumn(
                name: "IconType",
                table: "QuicklistEntry",
                newName: "Pos");
        }
    }
}
