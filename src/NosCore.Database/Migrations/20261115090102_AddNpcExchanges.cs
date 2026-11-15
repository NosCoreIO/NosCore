using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261115090102_AddNpcExchanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NpcExchange",
            columns: table => new
            {
                NpcExchangeId = table.Column<short>(type: "smallint", nullable: false),
                Runner = table.Column<short>(type: "smallint", nullable: false),
                ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                Amount = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NpcExchange", x => x.NpcExchangeId);
                table.ForeignKey(
                    name: "FK_NpcExchange_Item_ItemVNum",
                    column: x => x.ItemVNum,
                    principalTable: "Item",
                    principalColumn: "VNum",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "NpcExchangeMaterial",
            columns: table => new
            {
                NpcExchangeMaterialId = table.Column<short>(type: "smallint", nullable: false),
                NpcExchangeId = table.Column<short>(type: "smallint", nullable: false),
                ItemVNum = table.Column<short>(type: "smallint", nullable: false),
                Amount = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NpcExchangeMaterial", x => x.NpcExchangeMaterialId);
                table.ForeignKey(
                    name: "FK_NpcExchangeMaterial_Item_ItemVNum",
                    column: x => x.ItemVNum,
                    principalTable: "Item",
                    principalColumn: "VNum",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_NpcExchangeMaterial_NpcExchange_NpcExchangeId",
                    column: x => x.NpcExchangeId,
                    principalTable: "NpcExchange",
                    principalColumn: "NpcExchangeId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NpcExchange_ItemVNum",
            table: "NpcExchange",
            column: "ItemVNum");

        migrationBuilder.CreateIndex(
            name: "IX_NpcExchangeMaterial_ItemVNum",
            table: "NpcExchangeMaterial",
            column: "ItemVNum");

        migrationBuilder.CreateIndex(
            name: "IX_NpcExchangeMaterial_NpcExchangeId",
            table: "NpcExchangeMaterial",
            column: "NpcExchangeId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NpcExchangeMaterial");

        migrationBuilder.DropTable(
            name: "NpcExchange");
    }
}
