//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NosCore.Database
{
    // The migration history is squashed into one baseline from time to time. A database that
    // went through the whole squashed history already has the baseline's schema, so it is
    // recorded as applied instead of being created a second time.
    public static class MigrationBaseline
    {
        public const string LastSquashedMigration = "20261116014952_AddNpcMonsterOwnerBlessingCard";

        public static void AdoptSquashedHistory(this DatabaseFacade database)
        {
            var history = database.GetService<IHistoryRepository>();
            if (!history.Exists())
            {
                return;
            }

            if (Adoption(database, history.GetAppliedMigrations().Select(r => r.MigrationId).ToList()) is { } insert)
            {
                database.ExecuteSqlRaw(insert);
            }
        }

        public static async Task AdoptSquashedHistoryAsync(this DatabaseFacade database, CancellationToken cancellationToken = default)
        {
            var history = database.GetService<IHistoryRepository>();
            if (!await history.ExistsAsync(cancellationToken))
            {
                return;
            }

            var applied = (await history.GetAppliedMigrationsAsync(cancellationToken)).Select(r => r.MigrationId).ToList();
            if (Adoption(database, applied) is { } insert)
            {
                await database.ExecuteSqlRawAsync(insert, cancellationToken);
            }
        }

        private static string? Adoption(DatabaseFacade database, IReadOnlyCollection<string> applied)
        {
            var baseline = database.GetMigrations().First();
            if (applied.Count == 0 || applied.Contains(baseline))
            {
                return null;
            }

            if (!applied.Contains(LastSquashedMigration))
            {
                throw new DatabaseNotReadyException(
                    $"The database predates the migration baseline {baseline} and stops short of {LastSquashedMigration}. " +
                    "Update it with the parser of a build from before the baseline, then run this parser again.");
            }

            return database.GetService<IHistoryRepository>()
                .GetInsertScript(new HistoryRow(baseline, ProductInfo.GetVersion()));
        }
    }
}
