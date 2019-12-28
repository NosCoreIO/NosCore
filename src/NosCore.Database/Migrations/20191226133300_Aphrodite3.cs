using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database.Migrations
{
    public partial class Aphrodite3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(nullable: false),
                    TargetId = table.Column<string>(maxLength: 80, nullable: true),
                    TargetType = table.Column<string>(maxLength: 32, nullable: true),
                    Time = table.Column<DateTime>(nullable: false),
                    AuditLogType = table.Column<int>(nullable: false),
                    Value = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");
        }
    }
}
