using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterRelation",
                columns: table => new
                {
                    CharacterId = table.Column<long>(nullable: false),
                    CharacterRelationId = table.Column<Guid>(nullable: false),
                    RelatedCharacterId = table.Column<long>(nullable: false),
                    RelationType = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelation", x => x.CharacterRelationId);
                    table.ForeignKey(
                        name: "FK_CharacterRelation_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterRelation_Character_RelatedCharacterId",
                        column: x => x.RelatedCharacterId,
                        principalTable: "Character",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelation_CharacterId",
                table: "CharacterRelation",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelation_RelatedCharacterId",
                table: "CharacterRelation",
                column: "RelatedCharacterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterRelation");
        }
    }
}
