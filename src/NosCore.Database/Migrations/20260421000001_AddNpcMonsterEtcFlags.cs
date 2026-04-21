using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcMonsterEtcFlags : Migration
    {
        private static readonly string[] Columns =
        {
            "CanCollect", "CantDebuff", "CanCatch",
            "DisappearAfterSeconds", "DisappearAfterHitting", "HasMode",
            "DisappearAfterSecondsMana", "OnDefenseOnlyOnce", "HasDash",
            "RegenerateHpOverTime", "CantVoke", "DontDrainHpAfterSeconds",
            "CantTargetInfo"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.AddColumn<bool>(
                    name: column,
                    table: "NpcMonster",
                    type: "boolean",
                    nullable: false,
                    defaultValue: false);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.DropColumn(
                    name: column,
                    table: "NpcMonster");
            }
        }
    }
}
