using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingCharacterOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GroupSharing",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HatInvisible",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LockInterface",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PartnerAutoRelive",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PetAutoRelive",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupSharing",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "HatInvisible",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "LockInterface",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "PartnerAutoRelive",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "PetAutoRelive",
                table: "Character");
        }
    }
}
