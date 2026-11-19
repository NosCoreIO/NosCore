using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261119011938_RecordCompletedTimeSpaces : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "TimeSpaceId",
            table: "ScriptedInstance",
            type: "smallint",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CompletedTimeSpace",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CharacterId = table.Column<long>(type: "bigint", nullable: false),
                TimeSpaceId = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompletedTimeSpace", x => x.Id);
                table.ForeignKey(
                    name: "FK_CompletedTimeSpace_Character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Character",
                    principalColumn: "CharacterId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompletedTimeSpace_CharacterId_TimeSpaceId",
            table: "CompletedTimeSpace",
            columns: new[] { "CharacterId", "TimeSpaceId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CompletedTimeSpace");

        migrationBuilder.DropColumn(
            name: "TimeSpaceId",
            table: "ScriptedInstance");
    }
}
