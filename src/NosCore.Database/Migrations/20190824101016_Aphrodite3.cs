using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "SenderMorphId",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(short));

            migrationBuilder.AlterColumn<long>(
                name: "SenderId",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<byte>(
                name: "SenderHairStyle",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<byte>(
                name: "SenderHairColor",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<byte>(
                name: "SenderGender",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<byte>(
                name: "SenderCharacterClass",
                table: "Mail",
                nullable: true,
                oldClrType: typeof(byte));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "SenderMorphId",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(short),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SenderId",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "SenderHairStyle",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(byte),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "SenderHairColor",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(byte),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "SenderGender",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(byte),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "SenderCharacterClass",
                table: "Mail",
                nullable: false,
                oldClrType: typeof(byte),
                oldNullable: true);
        }
    }
}
