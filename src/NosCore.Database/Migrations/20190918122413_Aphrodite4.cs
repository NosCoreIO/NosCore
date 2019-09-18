using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "Rare",
                table: "ItemInstance",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<int>(
                name: "Effect",
                table: "Item",
                nullable: false,
                oldClrType: typeof(short));

            migrationBuilder.AddColumn<long>(
                name: "BankMoney",
                table: "Account",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ItemShopMoney",
                table: "Account",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rare",
                table: "ItemInstance");

            migrationBuilder.DropColumn(
                name: "BankMoney",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "ItemShopMoney",
                table: "Account");

            migrationBuilder.AlterColumn<short>(
                name: "Effect",
                table: "Item",
                nullable: false,
                oldClrType: typeof(int));
        }
    }
}
