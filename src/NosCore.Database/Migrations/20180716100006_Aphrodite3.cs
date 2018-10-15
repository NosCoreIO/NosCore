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

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drop_Item_ItemVNum",
                table: "Drop");

            migrationBuilder.DropForeignKey(
                name: "FK_RespawnMapType_Map_DefaultMapId",
                table: "RespawnMapType");

            migrationBuilder.RenameColumn(
                name: "DefaultMapId",
                table: "RespawnMapType",
                newName: "MapId");

            migrationBuilder.RenameIndex(
                name: "IX_RespawnMapType_DefaultMapId",
                table: "RespawnMapType",
                newName: "IX_RespawnMapType_MapId");

            migrationBuilder.RenameColumn(
                name: "ItemVNum",
                table: "Drop",
                newName: "VNum");

            migrationBuilder.RenameIndex(
                name: "IX_Drop_ItemVNum",
                table: "Drop",
                newName: "IX_Drop_VNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Drop_Item_VNum",
                table: "Drop",
                column: "VNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespawnMapType_Map_MapId",
                table: "RespawnMapType",
                column: "MapId",
                principalTable: "Map",
                principalColumn: "MapId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drop_Item_VNum",
                table: "Drop");

            migrationBuilder.DropForeignKey(
                name: "FK_RespawnMapType_Map_MapId",
                table: "RespawnMapType");

            migrationBuilder.RenameColumn(
                name: "MapId",
                table: "RespawnMapType",
                newName: "DefaultMapId");

            migrationBuilder.RenameIndex(
                name: "IX_RespawnMapType_MapId",
                table: "RespawnMapType",
                newName: "IX_RespawnMapType_DefaultMapId");

            migrationBuilder.RenameColumn(
                name: "VNum",
                table: "Drop",
                newName: "ItemVNum");

            migrationBuilder.RenameIndex(
                name: "IX_Drop_VNum",
                table: "Drop",
                newName: "IX_Drop_ItemVNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Drop_Item_ItemVNum",
                table: "Drop",
                column: "ItemVNum",
                principalTable: "Item",
                principalColumn: "VNum",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespawnMapType_Map_DefaultMapId",
                table: "RespawnMapType",
                column: "DefaultMapId",
                principalTable: "Map",
                principalColumn: "MapId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
