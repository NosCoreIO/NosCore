//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database;
using Npgsql;

namespace NosCore.Tests.Shared
{
    // A database that exists for one test: created empty, optionally migrated, dropped on dispose.
    // Without a PostgreSQL the test is reported as not run, unless NOSCORE_E2E_REQUIRED asks for one.
    public sealed class PostgresDatabase : IAsyncDisposable
    {
        private const string RequiredVariable = "NOSCORE_E2E_REQUIRED";

        private PostgresDatabase(string name)
        {
            Name = name;
            var options = new DbContextOptionsBuilder<NosCoreContext>();
            options.UseNosCoreDatabase(ConnectionString(name));
            Options = options.Options;
        }

        public string Name { get; }

        public DbContextOptions<NosCoreContext> Options { get; }

        public static async Task<PostgresDatabase> CreateAsync()
        {
            var database = await CreateEmptyAsync();
            try
            {
                await using var context = database.CreateContext();
                await context.GetService<IMigrator>().MigrateAsync();
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }

            return database;
        }

        public static async Task<PostgresDatabase> CreateEmptyAsync()
        {
            var database = new PostgresDatabase("noscore_test_" + Guid.NewGuid().ToString("N")[..12]);
            await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
            try
            {
                await connection.OpenAsync();
            }
            // A server that answers and then refuses is a failure, not an absence.
            catch (Exception ex) when (ex is System.Net.Sockets.SocketException or TimeoutException
                || (ex is NpgsqlException && ex is not PostgresException))
            {
                if (string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                Assert.Inconclusive("No PostgreSQL answered on DB_HOST (default localhost) with DB_USER and DB_PASSWORD. " +
                    $"Set {RequiredVariable}=true to fail instead.");
            }

            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database.Name}\"", connection);
            await create.ExecuteNonQueryAsync();
            return database;
        }

        public NosCoreContext CreateContext() => new(Options);

        public async ValueTask DisposeAsync()
        {
            await using (var pooled = new NpgsqlConnection(ConnectionString(Name)))
            {
                NpgsqlConnection.ClearPool(pooled);
            }

            await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
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
    }
}
