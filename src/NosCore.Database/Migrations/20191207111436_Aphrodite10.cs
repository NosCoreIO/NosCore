using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Act",
                columns: table => new
                {
                    ActId = table.Column<byte>(nullable: false),
                    Title = table.Column<string>(maxLength: 255, nullable: true),
                    Scene = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Act", x => x.ActId);
                });

            migrationBuilder.CreateTable(
                name: "ActPart",
                columns: table => new
                {
                    ActPartId = table.Column<byte>(nullable: false),
                    ActId = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActPart", x => x.ActPartId);
                    table.ForeignKey(
                        name: "FK_ActPart_Act_ActId",
                        column: x => x.ActId,
                        principalTable: "Act",
                        principalColumn: "ActId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterActPart",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterId = table.Column<long>(nullable: false),
                    ActPartId = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterActPart_ActPart_ActPartId",
                        column: x => x.ActPartId,
                        principalTable: "ActPart",
                        principalColumn: "ActPartId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterActPart_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActPart_ActId",
                table: "ActPart",
                column: "ActId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActPart_ActPartId",
                table: "CharacterActPart",
                column: "ActPartId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActPart_CharacterId",
                table: "CharacterActPart",
                column: "CharacterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterActPart");

            migrationBuilder.DropTable(
                name: "ActPart");

            migrationBuilder.DropTable(
                name: "Act");
        }
    }
}
