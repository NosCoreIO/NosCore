using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemInstance_CharacterId_Slot_Type",
                table: "ItemInstance");

            migrationBuilder.DropColumn(
                name: "BazaarItemId",
                table: "ItemInstance");

            migrationBuilder.DropColumn(
                name: "Rare",
                table: "ItemInstance");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "ItemInstance");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ItemInstance");

            migrationBuilder.AlterColumn<int>(
                name: "SecondaryElement",
                table: "Item",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<int>(
                name: "Element",
                table: "Item",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<int>(
                name: "MemberAuthorityType",
                table: "Family",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<int>(
                name: "ManagerAuthorityType",
                table: "Family",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.CreateTable(
                name: "InventoryItemInstance",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    ItemInstanceId = table.Column<Guid>(nullable: false),
                    Slot = table.Column<short>(nullable: false),
                    Type = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItemInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItemInstance_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItemInstance_ItemInstance_ItemInstanceId",
                        column: x => x.ItemInstanceId,
                        principalTable: "ItemInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_CharacterId",
                table: "ItemInstance",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemInstance_ItemInstanceId",
                table: "InventoryItemInstance",
                column: "ItemInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemInstance_CharacterId_Slot_Type",
                table: "InventoryItemInstance",
                columns: new[] { "CharacterId", "Slot", "Type" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItemInstance");

            migrationBuilder.DropIndex(
                name: "IX_ItemInstance_CharacterId",
                table: "ItemInstance");

            migrationBuilder.AddColumn<long>(
                name: "BazaarItemId",
                table: "ItemInstance",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Rare",
                table: "ItemInstance",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "Slot",
                table: "ItemInstance",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte>(
                name: "Type",
                table: "ItemInstance",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<byte>(
                name: "SecondaryElement",
                table: "Item",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<byte>(
                name: "Element",
                table: "Item",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<byte>(
                name: "MemberAuthorityType",
                table: "Family",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<byte>(
                name: "ManagerAuthorityType",
                table: "Family",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.CreateIndex(
                name: "IX_ItemInstance_CharacterId_Slot_Type",
                table: "ItemInstance",
                columns: new[] { "CharacterId", "Slot", "Type" },
                unique: true);
        }
    }
}
