using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReputationLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReputationLevel",
                columns: table => new
                {
                    ReputationLevelId = table.Column<byte>(type: "smallint", nullable: false),
                    MinReputation = table.Column<long>(type: "bigint", nullable: false),
                    MaxReputation = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationLevel", x => x.ReputationLevelId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReputationLevel");
        }
    }
}
