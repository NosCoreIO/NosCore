using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinilandObject_Character_CharacterId",
                table: "MinilandObject");

            migrationBuilder.DropIndex(
                name: "IX_MinilandObject_CharacterId",
                table: "MinilandObject");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "MinilandObject");

            migrationBuilder.DropColumn(
                name: "MinilandMessage",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "MinilandPoint",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "MinilandState",
                table: "Character");

            migrationBuilder.AddColumn<Guid>(
                name: "MinilandId",
                table: "MinilandObject",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Miniland",
                columns: table => new
                {
                    MinilandId = table.Column<Guid>(nullable: false),
                    MinilandMessage = table.Column<string>(maxLength: 255, nullable: true),
                    MinilandPoint = table.Column<long>(nullable: false),
                    State = table.Column<byte>(nullable: false),
                    OwnerId = table.Column<long>(nullable: false),
                    DailyVisitCount = table.Column<int>(nullable: false),
                    VisitCount = table.Column<int>(nullable: false),
                    WelcomeMusicInfo = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Miniland", x => x.MinilandId);
                    table.ForeignKey(
                        name: "FK_Miniland_Character_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_MinilandId",
                table: "MinilandObject",
                column: "MinilandId");

            migrationBuilder.CreateIndex(
                name: "IX_Miniland_OwnerId",
                table: "Miniland",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinilandObject_Miniland_MinilandId",
                table: "MinilandObject",
                column: "MinilandId",
                principalTable: "Miniland",
                principalColumn: "MinilandId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinilandObject_Miniland_MinilandId",
                table: "MinilandObject");

            migrationBuilder.DropTable(
                name: "Miniland");

            migrationBuilder.DropIndex(
                name: "IX_MinilandObject_MinilandId",
                table: "MinilandObject");

            migrationBuilder.DropColumn(
                name: "MinilandId",
                table: "MinilandObject");

            migrationBuilder.AddColumn<long>(
                name: "CharacterId",
                table: "MinilandObject",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "MinilandMessage",
                table: "Character",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "MinilandPoint",
                table: "Character",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte>(
                name: "MinilandState",
                table: "Character",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "IX_MinilandObject_CharacterId",
                table: "MinilandObject",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinilandObject_Character_CharacterId",
                table: "MinilandObject",
                column: "CharacterId",
                principalTable: "Character",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
