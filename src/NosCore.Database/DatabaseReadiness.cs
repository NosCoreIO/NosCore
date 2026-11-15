//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NosCore.Database
{
    // The servers do not migrate: the parser does. A server started on a database the parser
    // has not brought up to date stops here, with the reason, instead of failing on the first
    // query that meets a missing column.
    public static class DatabaseReadiness
    {
        public static void EnsureUpToDate(string connectionString)
        {
            var options = new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase(connectionString).Options;
            using var context = new NosCoreContext(options);
            List<string> pending;
            try
            {
                pending = context.Database.GetPendingMigrations().ToList();
            }
            catch (DbException e)
            {
                throw new DatabaseNotReadyException(
                    $"The database cannot be read ({e.Message}). Run the parser to create it, then start the server again.", e);
            }

            if (Describe(pending) is { } behind)
            {
                throw new DatabaseNotReadyException(behind);
            }
        }

        public static string? Describe(IReadOnlyList<string> pendingMigrations) => pendingMigrations.Count == 0
            ? null
            : $"The database is {pendingMigrations.Count} migration{(pendingMigrations.Count == 1 ? "" : "s")} behind this build " +
              $"(next: {pendingMigrations[0]}). Run the parser to update it, then start the server again.";
    }

    public sealed class DatabaseNotReadyException(string message, Exception? innerException = null)
        : InvalidOperationException(message, innerException);
}
