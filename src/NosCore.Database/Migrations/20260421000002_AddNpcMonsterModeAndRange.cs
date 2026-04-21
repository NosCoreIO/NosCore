using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcMonsterModeAndRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>("AlwaysActive", "NpcMonster", "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<byte>("Limiter", "NpcMonster", "smallint", nullable: false, defaultValue: (byte)0);
            migrationBuilder.AddColumn<short>("HpThreshold", "NpcMonster", "smallint", nullable: false, defaultValue: (short)0);
            migrationBuilder.AddColumn<short>("RangeThreshold", "NpcMonster", "smallint", nullable: false, defaultValue: (short)0);
            migrationBuilder.AddColumn<short>("CModeVNum", "NpcMonster", "smallint", nullable: false, defaultValue: (short)0);
            migrationBuilder.AddColumn<byte>("CellMinRange", "NpcMonster", "smallint", nullable: false, defaultValue: (byte)0);
            migrationBuilder.AddColumn<int>("Midgard", "NpcMonster", "integer", nullable: false, defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("AlwaysActive", "NpcMonster");
            migrationBuilder.DropColumn("Limiter", "NpcMonster");
            migrationBuilder.DropColumn("HpThreshold", "NpcMonster");
            migrationBuilder.DropColumn("RangeThreshold", "NpcMonster");
            migrationBuilder.DropColumn("CModeVNum", "NpcMonster");
            migrationBuilder.DropColumn("CellMinRange", "NpcMonster");
            migrationBuilder.DropColumn("Midgard", "NpcMonster");
        }
    }
}
