using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261114021543_ResyncRespawnIdentity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Rows used to be written with explicit ids, so the identity never advanced past them.
        migrationBuilder.Sql(
            "SELECT setval(pg_get_serial_sequence('\"Respawn\"', 'RespawnId'), COALESCE(MAX(\"RespawnId\"), 0) + 1, false) FROM \"Respawn\";");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
