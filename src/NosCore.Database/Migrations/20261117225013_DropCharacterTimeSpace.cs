using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261117225013_DropCharacterTimeSpace : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CharacterTimeSpace");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CharacterTimeSpace",
            columns: table => new
            {
                CharacterTimeSpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                CharacterId = table.Column<long>(type: "bigint", nullable: false),
                FailedAttempts = table.Column<short>(type: "smallint", nullable: false),
                TimeSpaceId = table.Column<short>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CharacterTimeSpace", x => x.CharacterTimeSpaceId);
                table.ForeignKey(
                    name: "FK_CharacterTimeSpace_Character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Character",
                    principalColumn: "CharacterId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CharacterTimeSpace_CharacterId_TimeSpaceId",
            table: "CharacterTimeSpace",
            columns: new[] { "CharacterId", "TimeSpaceId" },
            unique: true);
    }
}
