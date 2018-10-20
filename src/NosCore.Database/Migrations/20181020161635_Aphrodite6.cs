using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "I18N_ActDesc");

            migrationBuilder.DropTable(
                name: "I18N_BCard");

            migrationBuilder.DropTable(
                name: "I18N_Card");

            migrationBuilder.DropTable(
                name: "I18N_Item");

            migrationBuilder.DropTable(
                name: "I18N_MapIdData");

            migrationBuilder.DropTable(
                name: "I18N_MapPointData");

            migrationBuilder.DropTable(
                name: "I18N_NpcMonster");

            migrationBuilder.DropTable(
                name: "I18N_NpcMonsterTalk");

            migrationBuilder.DropTable(
                name: "I18N_Quest");

            migrationBuilder.DropTable(
                name: "I18N_Skill");

            migrationBuilder.RenameColumn(
                name: "CPCost",
                table: "Skill",
                newName: "CpCost");

            migrationBuilder.RenameColumn(
                name: "XP",
                table: "NpcMonster",
                newName: "Xp");

            migrationBuilder.RenameColumn(
                name: "MaxMP",
                table: "NpcMonster",
                newName: "MaxMp");

            migrationBuilder.RenameColumn(
                name: "MaxHP",
                table: "NpcMonster",
                newName: "MaxHp");

            migrationBuilder.RenameColumn(
                name: "JobXP",
                table: "NpcMonster",
                newName: "JobXp");

            migrationBuilder.RenameColumn(
                name: "HeroXP",
                table: "NpcMonster",
                newName: "HeroXp");

            migrationBuilder.RenameColumn(
                name: "XP",
                table: "ItemInstance",
                newName: "Xp");

            migrationBuilder.RenameColumn(
                name: "WearableInstance_MP",
                table: "ItemInstance",
                newName: "WearableInstance_Mp");

            migrationBuilder.RenameColumn(
                name: "WearableInstance_HP",
                table: "ItemInstance",
                newName: "WearableInstance_Hp");

            migrationBuilder.RenameColumn(
                name: "MP",
                table: "ItemInstance",
                newName: "Mp");

            migrationBuilder.RenameColumn(
                name: "HP",
                table: "ItemInstance",
                newName: "Hp");

            migrationBuilder.RenameColumn(
                name: "SpHP",
                table: "ItemInstance",
                newName: "SpHp");

            migrationBuilder.RenameColumn(
                name: "SlHP",
                table: "ItemInstance",
                newName: "SlHp");

            migrationBuilder.RenameColumn(
                name: "RegistrationIP",
                table: "Account",
                newName: "RegistrationIp");

            migrationBuilder.CreateTable(
                name: "I18NActDesc",
                columns: table => new
                {
                    I18NActDescId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NActDesc", x => x.I18NActDescId);
                });

            migrationBuilder.CreateTable(
                name: "I18NBCard",
                columns: table => new
                {
                    I18NBCardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NBCard", x => x.I18NBCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NCard",
                columns: table => new
                {
                    I18NCardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NCard", x => x.I18NCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18NItem",
                columns: table => new
                {
                    I18NItemId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NItem", x => x.I18NItemId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapIdData",
                columns: table => new
                {
                    I18NMapIdDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapIdData", x => x.I18NMapIdDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NMapPointData",
                columns: table => new
                {
                    I18NMapPointDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NMapPointData", x => x.I18NMapPointDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonster",
                columns: table => new
                {
                    I18NNpcMonsterId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonster", x => x.I18NNpcMonsterId);
                });

            migrationBuilder.CreateTable(
                name: "I18NNpcMonsterTalk",
                columns: table => new
                {
                    I18NNpcMonsterTalkId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NNpcMonsterTalk", x => x.I18NNpcMonsterTalkId);
                });

            migrationBuilder.CreateTable(
                name: "I18NQuest",
                columns: table => new
                {
                    I18NQuestId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NQuest", x => x.I18NQuestId);
                });

            migrationBuilder.CreateTable(
                name: "I18NSkill",
                columns: table => new
                {
                    I18NSkillId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18NSkill", x => x.I18NSkillId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_I18NActDesc_Key_RegionType",
                table: "I18NActDesc",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NBCard_Key_RegionType",
                table: "I18NBCard",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NCard_Key_RegionType",
                table: "I18NCard",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NItem_Key_RegionType",
                table: "I18NItem",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NMapIdData_Key_RegionType",
                table: "I18NMapIdData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NMapPointData_Key_RegionType",
                table: "I18NMapPointData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NNpcMonster_Key_RegionType",
                table: "I18NNpcMonster",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NNpcMonsterTalk_Key_RegionType",
                table: "I18NNpcMonsterTalk",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NQuest_Key_RegionType",
                table: "I18NQuest",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18NSkill_Key_RegionType",
                table: "I18NSkill",
                columns: new[] { "Key", "RegionType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "I18NActDesc");

            migrationBuilder.DropTable(
                name: "I18NBCard");

            migrationBuilder.DropTable(
                name: "I18NCard");

            migrationBuilder.DropTable(
                name: "I18NItem");

            migrationBuilder.DropTable(
                name: "I18NMapIdData");

            migrationBuilder.DropTable(
                name: "I18NMapPointData");

            migrationBuilder.DropTable(
                name: "I18NNpcMonster");

            migrationBuilder.DropTable(
                name: "I18NNpcMonsterTalk");

            migrationBuilder.DropTable(
                name: "I18NQuest");

            migrationBuilder.DropTable(
                name: "I18NSkill");

            migrationBuilder.RenameColumn(
                name: "CpCost",
                table: "Skill",
                newName: "CPCost");

            migrationBuilder.RenameColumn(
                name: "Xp",
                table: "NpcMonster",
                newName: "XP");

            migrationBuilder.RenameColumn(
                name: "MaxMp",
                table: "NpcMonster",
                newName: "MaxMP");

            migrationBuilder.RenameColumn(
                name: "MaxHp",
                table: "NpcMonster",
                newName: "MaxHP");

            migrationBuilder.RenameColumn(
                name: "JobXp",
                table: "NpcMonster",
                newName: "JobXP");

            migrationBuilder.RenameColumn(
                name: "HeroXp",
                table: "NpcMonster",
                newName: "HeroXP");

            migrationBuilder.RenameColumn(
                name: "Xp",
                table: "ItemInstance",
                newName: "XP");

            migrationBuilder.RenameColumn(
                name: "WearableInstance_Mp",
                table: "ItemInstance",
                newName: "WearableInstance_MP");

            migrationBuilder.RenameColumn(
                name: "WearableInstance_Hp",
                table: "ItemInstance",
                newName: "WearableInstance_HP");

            migrationBuilder.RenameColumn(
                name: "Mp",
                table: "ItemInstance",
                newName: "MP");

            migrationBuilder.RenameColumn(
                name: "Hp",
                table: "ItemInstance",
                newName: "HP");

            migrationBuilder.RenameColumn(
                name: "SpHp",
                table: "ItemInstance",
                newName: "SpHP");

            migrationBuilder.RenameColumn(
                name: "SlHp",
                table: "ItemInstance",
                newName: "SlHP");

            migrationBuilder.RenameColumn(
                name: "RegistrationIp",
                table: "Account",
                newName: "RegistrationIP");

            migrationBuilder.CreateTable(
                name: "I18N_ActDesc",
                columns: table => new
                {
                    I18N_ActDescId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_ActDesc", x => x.I18N_ActDescId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_BCard",
                columns: table => new
                {
                    I18N_BCardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_BCard", x => x.I18N_BCardId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_Card",
                columns: table => new
                {
                    I18N_CardId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_Card", x => x.I18N_CardId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_Item",
                columns: table => new
                {
                    I18N_ItemId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_Item", x => x.I18N_ItemId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_MapIdData",
                columns: table => new
                {
                    I18N_MapIdDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_MapIdData", x => x.I18N_MapIdDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_MapPointData",
                columns: table => new
                {
                    I18N_MapPointDataId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_MapPointData", x => x.I18N_MapPointDataId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_NpcMonster",
                columns: table => new
                {
                    I18N_NpcMonsterId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_NpcMonster", x => x.I18N_NpcMonsterId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_NpcMonsterTalk",
                columns: table => new
                {
                    I18N_NpcMonsterTalkId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_NpcMonsterTalk", x => x.I18N_NpcMonsterTalkId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_Quest",
                columns: table => new
                {
                    I18N_QuestId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_Quest", x => x.I18N_QuestId);
                });

            migrationBuilder.CreateTable(
                name: "I18N_Skill",
                columns: table => new
                {
                    I18N_SkillId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_I18N_Skill", x => x.I18N_SkillId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_I18N_ActDesc_Key_RegionType",
                table: "I18N_ActDesc",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_BCard_Key_RegionType",
                table: "I18N_BCard",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Card_Key_RegionType",
                table: "I18N_Card",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Item_Key_RegionType",
                table: "I18N_Item",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_MapIdData_Key_RegionType",
                table: "I18N_MapIdData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_MapPointData_Key_RegionType",
                table: "I18N_MapPointData",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_NpcMonster_Key_RegionType",
                table: "I18N_NpcMonster",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_NpcMonsterTalk_Key_RegionType",
                table: "I18N_NpcMonsterTalk",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Quest_Key_RegionType",
                table: "I18N_Quest",
                columns: new[] { "Key", "RegionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Skill_Key_RegionType",
                table: "I18N_Skill",
                columns: new[] { "Key", "RegionType" },
                unique: true);
        }
    }
}
