//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class MigrationTests
    {
        private static readonly MethodInfo ReadOneMethod = typeof(MigrationTests)
            .GetMethod(nameof(ReadOneAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ReadOneAsync is missing.");

        private PostgresDatabase _database;
        private NosCoreContext _context;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateEmptyAsync();
            _context = _database.CreateContext();
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
        public async Task EveryMigrationAppliesToAnEmptyDatabase()
        {
            await new Spec("Every migration applies to an empty database and leaves nothing pending")
                .WhenAsync(Migrating)
                .ThenAsync(EveryMigrationIsApplied)
                .AndAsync(NothingIsPending)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryEntityCanBeReadFromTheMigratedSchema()
        {
            await new Spec("Every table and column the model maps exists in the migrated database")
                .GivenAsync(Migrating)
                .ThenAsync(EveryEntityIsReadable)
                .ExecuteAsync();
        }

        private Task Migrating() => _context.GetService<IMigrator>().MigrateAsync();

        private async Task EveryMigrationIsApplied() =>
            CollectionAssert.AreEqual(_context.Database.GetMigrations().ToArray(),
                (await _context.Database.GetAppliedMigrationsAsync()).ToArray());

        private async Task NothingIsPending() =>
            Assert.IsFalse((await _context.Database.GetPendingMigrationsAsync()).Any());

        private async Task EveryEntityIsReadable()
        {
            var unreadable = new List<string>();
            foreach (var entityType in _context.Model.GetEntityTypes().Where(e => !e.IsOwned() && !e.HasSharedClrType))
            {
                try
                {
                    if (ReadOneMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [_context]) is Task read)
                    {
                        await read;
                    }
                }
                catch (Exception ex)
                {
                    unreadable.Add($"{entityType.DisplayName()}: {ex.Message}");
                }
            }

            Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, unreadable));
        }

        private static Task<List<T>> ReadOneAsync<T>(NosCoreContext context) where T : class =>
            context.Set<T>().AsNoTracking().Take(1).ToListAsync();
    }
}
