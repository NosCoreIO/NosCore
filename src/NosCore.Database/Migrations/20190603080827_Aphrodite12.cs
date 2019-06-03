using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance");

            migrationBuilder.DropIndex(
                name: "IX_ItemInstance_CharacterId",
                table: "ItemInstance");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_CharacterId",
                table: "ItemInstance",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
