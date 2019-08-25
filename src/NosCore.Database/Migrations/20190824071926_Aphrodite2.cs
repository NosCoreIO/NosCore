using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mail_Item_AttachmentVNum",
                table: "Mail");

            migrationBuilder.DropForeignKey(
                name: "FK_MinilandObject_Miniland_MinilandId",
                table: "MinilandObject");

            migrationBuilder.DropIndex(
                name: "IX_MinilandObject_MinilandId",
                table: "MinilandObject");

            migrationBuilder.DropColumn(
                name: "MinilandId",
                table: "MinilandObject");

            migrationBuilder.DropColumn(
                name: "AttachmentAmount",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "AttachmentRarity",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "AttachmentUpgrade",
                table: "Mail");

            migrationBuilder.RenameColumn(
                name: "AttachmentVNum",
                table: "Mail",
                newName: "ItemVNum");

            migrationBuilder.RenameIndex(
                name: "IX_Mail_AttachmentVNum",
                table: "Mail",
                newName: "IX_Mail_ItemVNum");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemInstanceId",
                table: "Mail",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Mail_ItemInstanceId",
                table: "Mail",
                column: "ItemInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mail_ItemInstance_ItemInstanceId",
                table: "Mail",
                column: "ItemInstanceId",
                principalTable: "ItemInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mail_Item_ItemVNum",
                table: "Mail",
                column: "ItemVNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mail_ItemInstance_ItemInstanceId",
                table: "Mail");

            migrationBuilder.DropForeignKey(
                name: "FK_Mail_Item_ItemVNum",
                table: "Mail");

            migrationBuilder.DropIndex(
                name: "IX_Mail_ItemInstanceId",
                table: "Mail");

            migrationBuilder.DropColumn(
                name: "ItemInstanceId",
                table: "Mail");

            migrationBuilder.RenameColumn(
                name: "ItemVNum",
                table: "Mail",
                newName: "AttachmentVNum");

            migrationBuilder.RenameIndex(
                name: "IX_Mail_ItemVNum",
                table: "Mail",
                newName: "IX_Mail_AttachmentVNum");

            migrationBuilder.AddColumn<Guid>(
                name: "MinilandId",
                table: "MinilandObject",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "AttachmentAmount",
                table: "Mail",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "AttachmentRarity",
                table: "Mail",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "AttachmentUpgrade",
                table: "Mail",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_MinilandId",
                table: "MinilandObject",
                column: "MinilandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mail_Item_AttachmentVNum",
                table: "Mail",
                column: "AttachmentVNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MinilandObject_Miniland_MinilandId",
                table: "MinilandObject",
                column: "MinilandId",
                principalTable: "Miniland",
                principalColumn: "MinilandId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
