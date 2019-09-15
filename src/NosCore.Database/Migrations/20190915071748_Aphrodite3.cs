using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mail_Item_ItemVNum",
                table: "Mail");

            migrationBuilder.DropIndex(
                name: "IX_Mail_ItemVNum",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "ItemVNum",
                table: "Mail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "ItemVNum",
                table: "Mail",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mail_ItemVNum",
                table: "Mail",
                column: "ItemVNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Mail_Item_ItemVNum",
                table: "Mail",
                column: "ItemVNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
