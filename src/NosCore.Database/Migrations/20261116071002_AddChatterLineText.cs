using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261116071002_AddChatterLineText : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<short>(
            name: "Message",
            table: "ChatterLine",
            type: "smallint",
            nullable: true,
            oldClrType: typeof(short),
            oldType: "smallint");

        migrationBuilder.AddColumn<string>(
            name: "Text",
            table: "ChatterLine",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Text",
            table: "ChatterLine");

        migrationBuilder.AlterColumn<short>(
            name: "Message",
            table: "ChatterLine",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0,
            oldClrType: typeof(short),
            oldType: "smallint",
            oldNullable: true);
    }
}
