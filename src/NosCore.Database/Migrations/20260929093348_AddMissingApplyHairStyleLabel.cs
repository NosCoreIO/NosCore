using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NosCore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingApplyHairStyleLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The model has declared apply_hair_style since ParseMonsterDatExtras, but no migration
            // ever added it to the database type, so appending any later label fails.
            migrationBuilder.Sql(
                "ALTER TYPE item_effect_type ADD VALUE IF NOT EXISTS 'apply_hair_style' AFTER 'apply_hair_die';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL cannot drop an enum label.
        }
    }
}
