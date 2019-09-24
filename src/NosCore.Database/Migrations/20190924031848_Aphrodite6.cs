using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WearableInstanceId",
                table: "EquipmentOption",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentOption_WearableInstanceId",
                table: "EquipmentOption",
                column: "WearableInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentOption_ItemInstance_WearableInstanceId",
                table: "EquipmentOption",
                column: "WearableInstanceId",
                principalTable: "ItemInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentOption_ItemInstance_WearableInstanceId",
                table: "EquipmentOption");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentOption_WearableInstanceId",
                table: "EquipmentOption");

            migrationBuilder.DropColumn(
                name: "WearableInstanceId",
                table: "EquipmentOption");
        }
    }
}
