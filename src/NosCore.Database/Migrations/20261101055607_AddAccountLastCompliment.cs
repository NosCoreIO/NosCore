using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLastCompliment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "LastCompliment",
                table: "Account",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCompliment",
                table: "Account");
        }
    }
}
