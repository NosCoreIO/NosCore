using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class BumpToEf8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WearableInstance_Mp",
                table: "ItemInstance",
                newName: "UsableInstance_Mp");

            migrationBuilder.RenameColumn(
                name: "WearableInstance_Hp",
                table: "ItemInstance",
                newName: "UsableInstance_Hp");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "ItemInstance",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UsableInstance_Mp",
                table: "ItemInstance",
                newName: "WearableInstance_Mp");

            migrationBuilder.RenameColumn(
                name: "UsableInstance_Hp",
                table: "ItemInstance",
                newName: "WearableInstance_Hp");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "ItemInstance",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);
        }
    }
}
