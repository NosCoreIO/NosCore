//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database;
using NosCore.EndToEnd.Tests.Infrastructure;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MigrationBaselineTests
    {
        private static async Task<TestDatabase> CreateAsync(int migrationsBehind = 0)
        {
            var database = await TestDatabase.TryCreateAsync(migrationsBehind);
            if (database == null)
            {
                Assert.Inconclusive("PostgreSQL is not available.");
            }

            return database;
        }

        private static int MigrationCount()
        {
            using var context = new NosCoreContext(
                new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase("Host=localhost").Options);
            return context.Database.GetMigrations().Count();
        }

        // The baseline's schema is the squashed history's, so a database left at the last
        // squashed migration is the baseline's schema with the old history rows.
        private static async Task<TestDatabase> DatabaseWithHistoryAsync(params string[] migrationIds)
        {
            var database = await CreateAsync();
            await using var context = database.CreateContext();
            await context.Database.ExecuteSqlRawAsync("DELETE FROM \"__EFMigrationsHistory\"");
            foreach (var id in migrationIds)
            {
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({id}, '10.0.0')");
            }

            return database;
        }

        [TestMethod]
        public async Task ADatabaseAtTheLastSquashedMigrationIsTakenAsTheBaseline()
        {
            await using var database = await DatabaseWithHistoryAsync(
                "20220103074410_FirstMigration", MigrationBaseline.LastSquashedMigration);
            await using var context = database.CreateContext();

            await context.Database.AdoptSquashedHistoryAsync();
            await context.Database.MigrateAsync();

            Assert.IsEmpty(await context.Database.GetPendingMigrationsAsync());
            CollectionAssert.Contains(
                (await context.Database.GetAppliedMigrationsAsync()).ToList(), context.Database.GetMigrations().First());
        }

        [TestMethod]
        public async Task ADatabaseStoppedBeforeTheLastSquashedMigrationIsRefusedWithTheReason()
        {
            await using var database = await DatabaseWithHistoryAsync("20220103074410_FirstMigration");
            await using var context = database.CreateContext();

            var refusal = await Assert.ThrowsExactlyAsync<DatabaseNotReadyException>(
                () => context.Database.AdoptSquashedHistoryAsync());

            StringAssert.Contains(refusal.Message, MigrationBaseline.LastSquashedMigration);
        }

        [TestMethod]
        public async Task AnEmptyDatabaseIsLeftToTheMigrations()
        {
            await using var database = await CreateAsync(migrationsBehind: MigrationCount());
            await using var context = database.CreateContext();

            await context.Database.AdoptSquashedHistoryAsync();
            await context.Database.MigrateAsync();

            Assert.IsEmpty(await context.Database.GetPendingMigrationsAsync());
        }

        [TestMethod]
        public async Task AnUpToDateDatabaseIsLeftAlone()
        {
            await using var database = await CreateAsync();
            await using var context = database.CreateContext();
            var before = (await context.Database.GetAppliedMigrationsAsync()).ToList();

            await context.Database.AdoptSquashedHistoryAsync();

            CollectionAssert.AreEqual(before, (await context.Database.GetAppliedMigrationsAsync()).ToList());
        }
    }
}
