using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drop_Item_ItemVNum",
                table: "Drop");

            migrationBuilder.DropForeignKey(
                name: "FK_RespawnMapType_Map_DefaultMapId",
                table: "RespawnMapType");

            migrationBuilder.RenameColumn(
                name: "DefaultMapId",
                table: "RespawnMapType",
                newName: "MapId");

            migrationBuilder.RenameIndex(
                name: "IX_RespawnMapType_DefaultMapId",
                table: "RespawnMapType",
                newName: "IX_RespawnMapType_MapId");

            migrationBuilder.RenameColumn(
                name: "ItemVNum",
                table: "Drop",
                newName: "VNum");

            migrationBuilder.RenameIndex(
                name: "IX_Drop_ItemVNum",
                table: "Drop",
                newName: "IX_Drop_VNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Drop_Item_VNum",
                table: "Drop",
                column: "VNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespawnMapType_Map_MapId",
                table: "RespawnMapType",
                column: "MapId",
                principalTable: "Map",
                principalColumn: "MapId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drop_Item_VNum",
                table: "Drop");

            migrationBuilder.DropForeignKey(
                name: "FK_RespawnMapType_Map_MapId",
                table: "RespawnMapType");

            migrationBuilder.RenameColumn(
                name: "MapId",
                table: "RespawnMapType",
                newName: "DefaultMapId");

            migrationBuilder.RenameIndex(
                name: "IX_RespawnMapType_MapId",
                table: "RespawnMapType",
                newName: "IX_RespawnMapType_DefaultMapId");

            migrationBuilder.RenameColumn(
                name: "VNum",
                table: "Drop",
                newName: "ItemVNum");

            migrationBuilder.RenameIndex(
                name: "IX_Drop_VNum",
                table: "Drop",
                newName: "IX_Drop_ItemVNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Drop_Item_ItemVNum",
                table: "Drop",
                column: "ItemVNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespawnMapType_Map_DefaultMapId",
                table: "RespawnMapType",
                column: "DefaultMapId",
                principalTable: "Map",
                principalColumn: "MapId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
