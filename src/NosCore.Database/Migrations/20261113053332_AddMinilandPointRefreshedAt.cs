using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace NosCore.Database.Migrations;

/// <inheritdoc />
public partial class _20261113053332_AddMinilandPointRefreshedAt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Instant>(
            name: "MinilandPointRefreshedAt",
            table: "Miniland",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MinilandPointRefreshedAt",
            table: "Miniland");
    }
}
