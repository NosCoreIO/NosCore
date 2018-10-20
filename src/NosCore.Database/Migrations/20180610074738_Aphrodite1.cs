//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                constraints: table => table.PrimaryKey("PK_I18N_ActDesc", x => x.I18N_ActDescId));

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
                constraints: table => table.PrimaryKey("PK_I18N_BCard", x => x.I18N_BCardId));

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
                constraints: table => table.PrimaryKey("PK_I18N_Card", x => x.I18N_CardId));

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
                constraints: table => table.PrimaryKey("PK_I18N_Item", x => x.I18N_ItemId));

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
                constraints: table => table.PrimaryKey("PK_I18N_MapIdData", x => x.I18N_MapIdDataId));

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
                constraints: table => table.PrimaryKey("PK_I18N_MapPointData", x => x.I18N_MapPointDataId));

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
                constraints: table => table.PrimaryKey("PK_I18N_NpcMonster", x => x.I18N_NpcMonsterId));

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
                constraints: table => table.PrimaryKey("PK_I18N_NpcMonsterTalk", x => x.I18N_NpcMonsterTalkId));

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
                constraints: table => table.PrimaryKey("PK_I18N_Quest", x => x.I18N_QuestId));

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
                constraints: table => table.PrimaryKey("PK_I18N_Skill", x => x.I18N_SkillId));

            migrationBuilder.CreateIndex(
                name: "IX_I18N_ActDesc_Key_RegionType",
                table: "I18N_ActDesc",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_BCard_Key_RegionType",
                table: "I18N_BCard",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Card_Key_RegionType",
                table: "I18N_Card",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Item_Key_RegionType",
                table: "I18N_Item",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_MapIdData_Key_RegionType",
                table: "I18N_MapIdData",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_MapPointData_Key_RegionType",
                table: "I18N_MapPointData",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_NpcMonster_Key_RegionType",
                table: "I18N_NpcMonster",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_NpcMonsterTalk_Key_RegionType",
                table: "I18N_NpcMonsterTalk",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Quest_Key_RegionType",
                table: "I18N_Quest",
                columns: new[] {"Key", "RegionType"},
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_I18N_Skill_Key_RegionType",
                table: "I18N_Skill",
                columns: new[] {"Key", "RegionType"},
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}