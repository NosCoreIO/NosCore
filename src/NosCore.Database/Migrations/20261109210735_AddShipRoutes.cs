using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddShipRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipRoute",
                columns: table => new
                {
                    ShipRouteId = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Runner = table.Column<short>(type: "smallint", nullable: false),
                    CancelRunner = table.Column<short>(type: "smallint", nullable: false),
                    SourceMapId = table.Column<short>(type: "smallint", nullable: false),
                    BoardingNpcVNum = table.Column<short>(type: "smallint", nullable: false),
                    BoardingDialog = table.Column<short>(type: "smallint", nullable: false),
                    ShipMapId = table.Column<short>(type: "smallint", nullable: false),
                    ShipX = table.Column<short>(type: "smallint", nullable: false),
                    ShipY = table.Column<short>(type: "smallint", nullable: false),
                    DestinationMapId = table.Column<short>(type: "smallint", nullable: false),
                    DestinationX = table.Column<short>(type: "smallint", nullable: false),
                    DestinationY = table.Column<short>(type: "smallint", nullable: false),
                    ReturnX = table.Column<short>(type: "smallint", nullable: false),
                    ReturnY = table.Column<short>(type: "smallint", nullable: false),
                    CaptainVNum = table.Column<short>(type: "smallint", nullable: false),
                    CaptainDialog = table.Column<short>(type: "smallint", nullable: false),
                    CaptainX = table.Column<short>(type: "smallint", nullable: false),
                    CaptainY = table.Column<short>(type: "smallint", nullable: false),
                    RequiredFaction = table.Column<byte>(type: "smallint", nullable: true),
                    DepartureIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    Fare = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipRoute", x => x.ShipRouteId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipRoute");
        }
    }
}
