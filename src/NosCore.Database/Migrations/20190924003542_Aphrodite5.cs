using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterSkill_Skill_SkillVNum",
                table: "CharacterSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_Combo_Skill_SkillVNum",
                table: "Combo");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentOption_ItemInstance_WearableInstanceId",
                table: "EquipmentOption");

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
                name: "IX_EquipmentOption_WearableInstanceId",
                table: "EquipmentOption");

            migrationBuilder.DropIndex(
                name: "IX_Combo_SkillVNum",
                table: "Combo");

            migrationBuilder.DropIndex(
                name: "IX_CharacterSkill_SkillVNum",
                table: "CharacterSkill");

            migrationBuilder.DropColumn(
                name: "WearableInstanceId",
                table: "EquipmentOption");

            migrationBuilder.AlterColumn<short>(
                name: "TeleporterId",
                table: "Teleporter",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "StaticBuffId",
                table: "StaticBuff",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "StaticBonusId",
                table: "StaticBonus",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShopSkillId",
                table: "ShopSkill",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "ShopSkill",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ShopItemId",
                table: "ShopItem",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShopId",
                table: "Shop",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "ScriptedInstanceId",
                table: "ScriptedInstance",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RollGeneratedItemId",
                table: "RollGeneratedItem",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "RespawnId",
                table: "Respawn",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RecipeItemId",
                table: "RecipeItem",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RecipeId",
                table: "Recipe",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "QuestRewardId",
                table: "QuestReward",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "QuestObjectiveId",
                table: "QuestObjective",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PortalId",
                table: "Portal",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PenaltyLogId",
                table: "PenaltyLog",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "NpcMonsterSkillId",
                table: "NpcMonsterSkill",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<short>(
                name: "NpcMonsterVNum1",
                table: "NpcMonsterSkill",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "NpcMonsterSkill",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "MateId",
                table: "Mate",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "MapTypeMapId",
                table: "MapTypeMap",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "MailId",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NSkillId",
                table: "I18NSkill",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NQuestId",
                table: "I18NQuest",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NNpcMonsterTalkId",
                table: "I18NNpcMonsterTalk",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NNpcMonsterId",
                table: "I18NNpcMonster",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NMapPointDataId",
                table: "I18NMapPointData",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NMapIdDataId",
                table: "I18NMapIdData",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NItemId",
                table: "I18NItem",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NCardId",
                table: "I18NCard",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NbCardId",
                table: "I18NBCard",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NActDescId",
                table: "I18NActDesc",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyLogId",
                table: "FamilyLog",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyCharacterId",
                table: "FamilyCharacter",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyId",
                table: "Family",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "DropId",
                table: "Drop",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ComboId",
                table: "Combo",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "Combo",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "SkillVNum1",
                table: "CharacterSkill",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "Character",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<short>(
                name: "BCardId",
                table: "BCard",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "BazaarItemId",
                table: "BazaarItem",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<long>(
                name: "AccountId",
                table: "Account",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

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

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<short>(
                name: "TeleporterId",
                table: "Teleporter",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "StaticBuffId",
                table: "StaticBuff",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "StaticBonusId",
                table: "StaticBonus",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShopSkillId",
                table: "ShopSkill",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShopItemId",
                table: "ShopItem",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ShopId",
                table: "Shop",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "ScriptedInstanceId",
                table: "ScriptedInstance",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RollGeneratedItemId",
                table: "RollGeneratedItem",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "RespawnId",
                table: "Respawn",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RecipeItemId",
                table: "RecipeItem",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "RecipeId",
                table: "Recipe",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "QuestRewardId",
                table: "QuestReward",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "QuestObjectiveId",
                table: "QuestObjective",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PortalId",
                table: "Portal",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "PenaltyLogId",
                table: "PenaltyLog",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "NpcMonsterSkillId",
                table: "NpcMonsterSkill",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "MateId",
                table: "Mate",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "MapTypeMapId",
                table: "MapTypeMap",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "MailId",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NSkillId",
                table: "I18NSkill",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NQuestId",
                table: "I18NQuest",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NNpcMonsterTalkId",
                table: "I18NNpcMonsterTalk",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NNpcMonsterId",
                table: "I18NNpcMonster",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NMapPointDataId",
                table: "I18NMapPointData",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NMapIdDataId",
                table: "I18NMapIdData",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NItemId",
                table: "I18NItem",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NCardId",
                table: "I18NCard",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NbCardId",
                table: "I18NBCard",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "I18NActDescId",
                table: "I18NActDesc",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyLogId",
                table: "FamilyLog",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyCharacterId",
                table: "FamilyCharacter",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "FamilyId",
                table: "Family",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Guid>(
                name: "WearableInstanceId",
                table: "EquipmentOption",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<short>(
                name: "DropId",
                table: "Drop",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "ComboId",
                table: "Combo",
                nullable: false,
                oldClrType: typeof(int))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "CharacterId",
                table: "Character",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<short>(
                name: "BCardId",
                table: "BCard",
                nullable: false,
                oldClrType: typeof(short))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "BazaarItemId",
                table: "BazaarItem",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "AccountId",
                table: "Account",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
                name: "IX_EquipmentOption_WearableInstanceId",
                table: "EquipmentOption",
                column: "WearableInstanceId");

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
                name: "FK_EquipmentOption_ItemInstance_WearableInstanceId",
                table: "EquipmentOption",
                column: "WearableInstanceId",
                principalTable: "ItemInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
    }
}
