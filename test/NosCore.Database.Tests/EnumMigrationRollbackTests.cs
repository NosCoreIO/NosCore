//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using SpecLight;

namespace NosCore.Database.Tests
{
    // Runs on a database of its own, created and dropped by the test. Without a PostgreSQL the
    // test is reported as not run, unless NOSCORE_E2E_REQUIRED asks for one.
    [TestClass]
    public class EnumMigrationRollbackTests
    {
        private string _database = "";
        private bool _created;
        private IMigrator? _migrator;
        private NosCoreContext? _context;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = "noscore_rollback_" + Guid.NewGuid().ToString("N")[..8];
            await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex) when (ex is System.Net.Sockets.SocketException or TimeoutException
                || (ex is NpgsqlException && ex is not PostgresException))
            {
                if (string.Equals(Environment.GetEnvironmentVariable("NOSCORE_E2E_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                Assert.Inconclusive("No PostgreSQL answered on DB_HOST (default localhost) with DB_USER and DB_PASSWORD.");
                return;
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", connection);
            await create.ExecuteNonQueryAsync();
            _created = true;
            _context = new NosCoreContext(new DbContextOptionsBuilder<NosCoreContext>()
                .UseNosCoreDatabase(ConnectionString(_database)).Options);
            _migrator = _context.GetService<IMigrator>();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }

            if (!_created)
            {
                return;
            }

            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
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

        private static string ConnectionString(string database) => new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST") is { Length: > 0 } host ? host : "localhost",
            Port = 5432,
            Username = Environment.GetEnvironmentVariable("DB_USER") is { Length: > 0 } user ? user : "postgres",
            Password = Environment.GetEnvironmentVariable("DB_PASSWORD") is { Length: > 0 } password ? password : "password",
            Database = database,
            Timeout = 3
        }.ConnectionString;

        private NosCoreContext Context => _context ?? throw new InvalidOperationException("No database");

        private IMigrator Migrator => _migrator ?? throw new InvalidOperationException("No database");

        private Task TheDatabaseIsMigrated() => Migrator.MigrateAsync();

        private Task RollingBackToTheBaseline() => Migrator.MigrateAsync(Context.Database.GetMigrations().First());

        private async Task OnlyTheBaselineIsApplied() =>
            CollectionAssert.AreEqual(new[] { Context.Database.GetMigrations().First() },
                (await Context.Database.GetAppliedMigrationsAsync()).ToArray());

        private async Task TheTimeSpaceStoneLabelIsStillThere()
        {
            var labels = await Labels("item_effect_type");
            CollectionAssert.Contains(labels, "time_space_stone");
        }

        private async Task MigratingAgainLeavesNothingPending()
        {
            await Migrator.MigrateAsync();
            Assert.IsFalse((await Context.Database.GetPendingMigrationsAsync()).Any());
        }

        private async Task<List<string>> Labels(string type)
        {
            await using var connection = new NpgsqlConnection(ConnectionString(_database));
            await connection.OpenAsync();
            await using var query = new NpgsqlCommand(
                "SELECT e.enumlabel FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid WHERE t.typname = @type", connection);
            query.Parameters.AddWithValue("type", type);
            var labels = new List<string>();
            await using var reader = await query.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                labels.Add(reader.GetString(0));
            }

            return labels;
        }
    }
}
