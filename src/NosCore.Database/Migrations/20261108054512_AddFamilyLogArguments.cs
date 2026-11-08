using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyLogArguments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstArgument",
                table: "FamilyLog",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FourthArgument",
                table: "FamilyLog",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondArgument",
                table: "FamilyLog",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThirdArgument",
                table: "FamilyLog",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstArgument",
                table: "FamilyLog");

            migrationBuilder.DropColumn(
                name: "FourthArgument",
                table: "FamilyLog");

            migrationBuilder.DropColumn(
                name: "SecondArgument",
                table: "FamilyLog");

            migrationBuilder.DropColumn(
                name: "ThirdArgument",
                table: "FamilyLog");
        }
    }
}
