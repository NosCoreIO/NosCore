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
                name: "I18NActDesc",
                columns: table => new
                {
                    I18NActDescId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Key = table.Column<string>(nullable: true),
                    RegionType = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_I18NActDesc", x => x.I18NActDescId));

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
                constraints: table => table.PrimaryKey("PK_I18NBCard", x => x.I18NBCardId));

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
                constraints: table => table.PrimaryKey("PK_I18NCard", x => x.I18NCardId));

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
                constraints: table => table.PrimaryKey("PK_I18NItem", x => x.I18NItemId));

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
                constraints: table => table.PrimaryKey("PK_I18NMapIdData", x => x.I18NMapIdDataId));

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
                constraints: table => table.PrimaryKey("PK_I18NMapPointData", x => x.I18NMapPointDataId));

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
                constraints: table => table.PrimaryKey("PK_I18NNpcMonster", x => x.I18NNpcMonsterId));

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
                constraints: table => table.PrimaryKey("PK_I18NNpcMonsterTalk", x => x.I18NNpcMonsterTalkId));

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
                constraints: table => table.PrimaryKey("PK_I18NQuest", x => x.I18NQuestId));

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
                constraints: table => table.PrimaryKey("PK_I18NSkill", x => x.I18NSkillId));

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