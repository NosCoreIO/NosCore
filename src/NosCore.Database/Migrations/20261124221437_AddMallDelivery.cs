using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261124221437_AddMallDelivery : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MallDelivery",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CharacterId = table.Column<long>(type: "bigint", nullable: false),
                VNum = table.Column<short>(type: "smallint", nullable: false),
                Amount = table.Column<short>(type: "smallint", nullable: false),
                CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                DeliveredAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MallDelivery", x => x.Id);
                table.ForeignKey(
                    name: "FK_MallDelivery_Character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Character",
                    principalColumn: "CharacterId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MallDelivery_CharacterId",
            table: "MallDelivery",
            column: "CharacterId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MallDelivery");
    }
}
