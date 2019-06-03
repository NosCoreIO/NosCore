using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItemInstance_Character_CharacterId",
                table: "InventoryItemInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                table: "InventoryItemInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItemInstance_Character_CharacterId",
                table: "InventoryItemInstance",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                table: "InventoryItemInstance",
                column: "ItemInstanceId",
                principalTable: "ItemInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItemInstance_Character_CharacterId",
                table: "InventoryItemInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                table: "InventoryItemInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItemInstance_Character_CharacterId",
                table: "InventoryItemInstance",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                table: "InventoryItemInstance",
                column: "ItemInstanceId",
                principalTable: "ItemInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemInstance_Character_CharacterId",
                table: "ItemInstance",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
