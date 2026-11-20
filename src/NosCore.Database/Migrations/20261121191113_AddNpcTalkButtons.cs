using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261121191113_AddNpcTalkButtons : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NpcTalkButton",
            columns: table => new
            {
                NpcTalkButtonId = table.Column<Guid>(type: "uuid", nullable: false),
                DialogId = table.Column<short>(type: "smallint", nullable: false),
                Runner = table.Column<short>(type: "smallint", nullable: true),
                Argument = table.Column<short>(type: "smallint", nullable: false),
                NextDialogId = table.Column<short>(type: "smallint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NpcTalkButton", x => x.NpcTalkButtonId);
                table.ForeignKey(
                    name: "FK_NpcTalkButton_NpcTalk_DialogId",
                    column: x => x.DialogId,
                    principalTable: "NpcTalk",
                    principalColumn: "DialogId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NpcTalkButton_DialogId",
            table: "NpcTalkButton",
            column: "DialogId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NpcTalkButton");
    }
}
