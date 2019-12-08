using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActPart_Act_Id",
                table: "ActPart");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterActPart_ActPart_ActPartId",
                table: "CharacterActPart");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActPart",
                table: "ActPart");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ActPart");

            migrationBuilder.AddColumn<byte>(
                name: "ActPartNumber",
                table: "ActPart",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActPart",
                table: "ActPart",
                column: "ActPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ActPart_ActId",
                table: "ActPart",
                column: "ActId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActPart_Act_ActId",
                table: "ActPart",
                column: "ActId",
                principalTable: "Act",
                principalColumn: "ActId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterActPart_ActPart_ActPartId",
                table: "CharacterActPart",
                column: "ActPartId",
                principalTable: "ActPart",
                principalColumn: "ActPartId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActPart_Act_ActId",
                table: "ActPart");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterActPart_ActPart_ActPartId",
                table: "CharacterActPart");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActPart",
                table: "ActPart");

            migrationBuilder.DropIndex(
                name: "IX_ActPart_ActId",
                table: "ActPart");

            migrationBuilder.DropColumn(
                name: "ActPartNumber",
                table: "ActPart");

            migrationBuilder.AddColumn<byte>(
                name: "Id",
                table: "ActPart",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActPart",
                table: "ActPart",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActPart_Act_Id",
                table: "ActPart",
                column: "Id",
                principalTable: "Act",
                principalColumn: "ActId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterActPart_ActPart_ActPartId",
                table: "CharacterActPart",
                column: "ActPartId",
                principalTable: "ActPart",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
