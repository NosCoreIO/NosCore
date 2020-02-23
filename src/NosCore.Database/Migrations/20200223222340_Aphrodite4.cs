using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterSkill_Skill_SkillVNum1",
                table: "CharacterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_Combo_Skill_SkillVNum1",
                table: "Combo");

            migrationBuilder.DropForeignKey(
                name: "FK_NpcMonsterSkill_NpcMonster_NpcMonsterVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_NpcMonsterSkill_Skill_SkillVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopSkill_Skill_SkillVNum1",
                table: "ShopSkill");

            migrationBuilder.DropIndex(
                name: "IX_ShopSkill_SkillVNum1",
                table: "ShopSkill");

            migrationBuilder.DropIndex(
                name: "IX_NpcMonsterSkill_NpcMonsterVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropIndex(
                name: "IX_NpcMonsterSkill_SkillVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropIndex(
                name: "IX_Combo_SkillVNum1",
                table: "Combo");

            migrationBuilder.DropIndex(
                name: "IX_CharacterSkill_SkillVNum1",
                table: "CharacterSkill");

            migrationBuilder.DropColumn(
                name: "SkillVNum1",
                table: "ShopSkill");

            migrationBuilder.DropColumn(
                name: "NpcMonsterVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropColumn(
                name: "SkillVNum1",
                table: "NpcMonsterSkill");

            migrationBuilder.DropColumn(
                name: "SkillVNum1",
                table: "Combo");

            migrationBuilder.DropColumn(
                name: "SkillVNum1",
                table: "CharacterSkill");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSkill_SkillVNum",
                table: "ShopSkill",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_NpcMonsterVNum",
                table: "NpcMonsterSkill",
                column: "NpcMonsterVNum");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_SkillVNum",
                table: "NpcMonsterSkill",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_Combo_SkillVNum",
                table: "Combo",
                column: "SkillVNum");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_SkillVNum",
                table: "CharacterSkill",
                column: "SkillVNum");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterSkill_Skill_SkillVNum",
                table: "CharacterSkill",
                column: "SkillVNum",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Combo_Skill_SkillVNum",
                table: "Combo",
                column: "SkillVNum",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NpcMonsterSkill_NpcMonster_NpcMonsterVNum",
                table: "NpcMonsterSkill",
                column: "NpcMonsterVNum",
                principalTable: "NpcMonster",
                principalColumn: "NpcMonsterVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NpcMonsterSkill_Skill_SkillVNum",
                table: "NpcMonsterSkill",
                column: "SkillVNum",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopSkill_Skill_SkillVNum",
                table: "ShopSkill",
                column: "SkillVNum",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterSkill_Skill_SkillVNum",
                table: "CharacterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_Combo_Skill_SkillVNum",
                table: "Combo");

            migrationBuilder.DropForeignKey(
                name: "FK_NpcMonsterSkill_NpcMonster_NpcMonsterVNum",
                table: "NpcMonsterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_NpcMonsterSkill_Skill_SkillVNum",
                table: "NpcMonsterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopSkill_Skill_SkillVNum",
                table: "ShopSkill");

            migrationBuilder.DropIndex(
                name: "IX_ShopSkill_SkillVNum",
                table: "ShopSkill");

            migrationBuilder.DropIndex(
                name: "IX_NpcMonsterSkill_NpcMonsterVNum",
                table: "NpcMonsterSkill");

            migrationBuilder.DropIndex(
                name: "IX_NpcMonsterSkill_SkillVNum",
                table: "NpcMonsterSkill");

            migrationBuilder.DropIndex(
                name: "IX_Combo_SkillVNum",
                table: "Combo");

            migrationBuilder.DropIndex(
                name: "IX_CharacterSkill_SkillVNum",
                table: "CharacterSkill");

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "ShopSkill",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "NpcMonsterVNum1",
                table: "NpcMonsterSkill",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "NpcMonsterSkill",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "Combo",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "CharacterSkill",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopSkill_SkillVNum1",
                table: "ShopSkill",
                column: "SkillVNum1");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_NpcMonsterVNum1",
                table: "NpcMonsterSkill",
                column: "NpcMonsterVNum1");

            migrationBuilder.CreateIndex(
                name: "IX_NpcMonsterSkill_SkillVNum1",
                table: "NpcMonsterSkill",
                column: "SkillVNum1");

            migrationBuilder.CreateIndex(
                name: "IX_Combo_SkillVNum1",
                table: "Combo",
                column: "SkillVNum1");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkill_SkillVNum1",
                table: "CharacterSkill",
                column: "SkillVNum1");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterSkill_Skill_SkillVNum1",
                table: "CharacterSkill",
                column: "SkillVNum1",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Combo_Skill_SkillVNum1",
                table: "Combo",
                column: "SkillVNum1",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NpcMonsterSkill_NpcMonster_NpcMonsterVNum1",
                table: "NpcMonsterSkill",
                column: "NpcMonsterVNum1",
                principalTable: "NpcMonster",
                principalColumn: "NpcMonsterVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NpcMonsterSkill_Skill_SkillVNum1",
                table: "NpcMonsterSkill",
                column: "SkillVNum1",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopSkill_Skill_SkillVNum1",
                table: "ShopSkill",
                column: "SkillVNum1",
                principalTable: "Skill",
                principalColumn: "SkillVNum",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
