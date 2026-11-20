//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class EnumMigrationRollbackTests
    {
        private PostgresDatabase _database;
        private NosCoreContext _context;
        private IMigrator _migrator;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateEmptyAsync();
            _context = _database.CreateContext();
            _migrator = _context.GetService<IMigrator>();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_database is { } database)
            {
                await _context.DisposeAsync();
                await database.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task EveryMigrationSinceTheBaselineRollsBackAndAppliesAgain()
        {
            await new Spec("Rolling back past migrations that add enum labels keeps the labels, and applying them again works")
                .GivenAsync(TheDatabaseIsMigrated)
                .WhenAsync(RollingBackToTheBaseline)
                .ThenAsync(OnlyTheBaselineIsApplied)
                .AndAsync(TheTimeSpaceStoneLabelIsStillThere)
                .AndAsync(MigratingAgainLeavesNothingPending)
                .ExecuteAsync();
        }

        private Task TheDatabaseIsMigrated() => _migrator.MigrateAsync();

        private Task RollingBackToTheBaseline() => _migrator.MigrateAsync(_context.Database.GetMigrations().First());

        private async Task OnlyTheBaselineIsApplied() =>
            CollectionAssert.AreEqual(new[] { _context.Database.GetMigrations().First() },
                (await _context.Database.GetAppliedMigrationsAsync()).ToArray());

        private async Task TheTimeSpaceStoneLabelIsStillThere()
        {
            var labels = await Labels("item_effect_type");
            CollectionAssert.Contains(labels, "time_space_stone");
        }

        private async Task MigratingAgainLeavesNothingPending()
        {
            await _migrator.MigrateAsync();
            Assert.IsFalse((await _context.Database.GetPendingMigrationsAsync()).Any());
        }

        private Task<List<string>> Labels(string type) => _context.Database
            .SqlQuery<string>($"SELECT e.enumlabel AS \"Value\" FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid WHERE t.typname = {type}")
            .ToListAsync();
    }
}
