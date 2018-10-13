using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterRelation");

            migrationBuilder.AlterColumn<short>(
                name: "Amount",
                table: "ItemInstance",
                nullable: false,
                oldClrType: typeof(int));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "ItemInstance",
                nullable: false,
                oldClrType: typeof(short));

            migrationBuilder.CreateTable(
                name: "CharacterRelation",
                columns: table => new
                {
                    CharacterRelationId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CharacterId = table.Column<long>(nullable: false),
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
    }
}
