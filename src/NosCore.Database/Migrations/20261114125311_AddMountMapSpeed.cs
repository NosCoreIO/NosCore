using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261114125311_AddMountMapSpeed : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MountMapSpeed",
            columns: table => new
            {
                MountMapSpeedId = table.Column<int>(type: "integer", nullable: false),
                ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                MapId = table.Column<short>(type: "smallint", nullable: false),
                Speed = table.Column<byte>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MountMapSpeed", x => x.MountMapSpeedId);
                table.ForeignKey(
                    name: "FK_MountMapSpeed_Item_ItemVNum",
                    column: x => x.ItemVNum,
                    principalTable: "Item",
                    principalColumn: "VNum",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MountMapSpeed_ItemVNum",
            table: "MountMapSpeed",
            column: "ItemVNum");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MountMapSpeed");
    }
}
