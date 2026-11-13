using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261113152606_PartnerSkillSet : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "PartnerSkillSet",
            table: "Item",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PartnerSkillSet",
            table: "Item");
    }
}
