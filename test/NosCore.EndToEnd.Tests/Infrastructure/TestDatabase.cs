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
        private readonly string _server;

        private TestDatabase(string server, string name)
        {
            _server = server;
            Name = name;
        }

        public string Name { get; }

        private string ConnectionString => $"{_server}Database={Name};";

        // The same defaults and environment variables the servers' own configuration reads.
        private static string ServerConnectionString()
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") is { Length: > 0 } h ? h : "localhost";
            var user = Environment.GetEnvironmentVariable("DB_USER") is { Length: > 0 } u ? u : "postgres";
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") is { Length: > 0 } p ? p : "password";
            return $"Host={host};Port=5432;Username={user};Password={password};Timeout=3;";
        }

        // Null when no PostgreSQL answers: the tests then report themselves as not run.
        public static async Task<TestDatabase?> TryCreateAsync()
        {
            var server = ServerConnectionString();
            var name = "noscore_e2e_" + Guid.NewGuid().ToString("N")[..8];
            try
            {
                await using var connection = new NpgsqlConnection(server + "Database=postgres;");
                await connection.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
                await create.ExecuteNonQueryAsync();
            }
            catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
            {
                return null;
            }

            var database = new TestDatabase(server, name);
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public NosCoreContext CreateContext() =>
            new(new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase(ConnectionString).Options);

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(_server + "Database=postgres;");
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
