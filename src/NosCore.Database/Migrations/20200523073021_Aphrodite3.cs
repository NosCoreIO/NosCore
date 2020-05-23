using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "Dignity",
                table: "Character",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Dignity",
                table: "Character",
                type: "real",
                nullable: false,
                oldClrType: typeof(short));
        }
    }
}
