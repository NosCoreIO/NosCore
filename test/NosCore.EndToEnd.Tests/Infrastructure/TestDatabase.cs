//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NosCore.Database;
using Npgsql;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // A database that exists for one test run: created empty, migrated, seeded, dropped.
    public sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string name)
        {
            Name = name;
        }

        public string Name { get; }

        // The same defaults and environment variables the servers' own configuration reads.
        private static string ConnectionString(string database) => new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST") is { Length: > 0 } host ? host : "localhost",
            Port = 5432,
            Username = Environment.GetEnvironmentVariable("DB_USER") is { Length: > 0 } user ? user : "postgres",
            Password = Environment.GetEnvironmentVariable("DB_PASSWORD") is { Length: > 0 } password ? password : "password",
            Database = database,
            Timeout = 3
        }.ConnectionString;

        // Null when no PostgreSQL answers: the tests then report themselves as not run. A server
        // that answers and then refuses is a failure, not an absence.
        public static async Task<TestDatabase?> TryCreateAsync()
        {
            var database = new TestDatabase("noscore_e2e_" + Guid.NewGuid().ToString("N")[..8]);
            await using (var connection = new NpgsqlConnection(ConnectionString("postgres")))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex) when (ex is System.Net.Sockets.SocketException or TimeoutException
                    || (ex is NpgsqlException && ex is not PostgresException))
                {
                    return null;
                }

                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database.Name}\"", connection);
                await create.ExecuteNonQueryAsync();
            }

            try
            {
                await using var context = database.CreateContext();
                await context.Database.MigrateAsync();
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }

            return database;
        }

        public NosCoreContext CreateContext() =>
            new(new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase(ConnectionString(Name)).Options);

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
