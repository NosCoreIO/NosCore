using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261124224259_KeepTimeSpaceRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TimeSpaceRecord",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CharacterId = table.Column<long>(type: "bigint", nullable: false),
                TimeSpaceId = table.Column<short>(type: "smallint", nullable: false),
                Score = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeSpaceRecord", x => x.Id);
                table.ForeignKey(
                    name: "FK_TimeSpaceRecord_Character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Character",
                    principalColumn: "CharacterId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TimeSpaceRecord_CharacterId",
            table: "TimeSpaceRecord",
            column: "CharacterId");

        migrationBuilder.CreateIndex(
            name: "IX_TimeSpaceRecord_TimeSpaceId",
            table: "TimeSpaceRecord",
            column: "TimeSpaceId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TimeSpaceRecord");
    }
}
