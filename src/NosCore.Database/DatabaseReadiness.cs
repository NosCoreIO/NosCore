//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using NosCore.Database.Entities;
using Npgsql;

namespace NosCore.Database
{
    // The servers do not migrate: the parser does. A server started on a database the parser
    // has not brought up to date stops here, with the reason, instead of failing on the first
    // query that meets a missing column.
    public static class DatabaseReadiness
    {
        private const int GameDataImportRow = 1;
        private const int GameDataImportLockKey = 0x4E6F7343;

        public static void EnsureUpToDate(string connectionString)
        {
            List<string> pending;
            try
            {
                pending = PendingMigrations(connectionString);
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

        // Master and login start in any order with the world server, which imports the game data
        // on a fresh database, so they wait for it rather than stop.
        public static void WaitUntilUpToDate(string connectionString)
        {
            string? reported = null;
            while (true)
            {
                try
                {
                    EnsureUpToDate(connectionString);
                    return;
                }
                catch (DatabaseNotReadyException e)
                {
                    if (e.Message != reported)
                    {
                        Console.WriteLine($"Waiting for a world server or the parser to bring the database up to date. {e.Message}");
                        reported = e.Message;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        public static bool IsUpToDate(string connectionString)
        {
            try
            {
                return PendingMigrations(connectionString).Count == 0;
            }
            catch (DbException)
            {
                return false;
            }
        }

        private static List<string> PendingMigrations(string connectionString)
        {
            var options = new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase(connectionString).Options;
            using var context = new NosCoreContext(options);
            return context.Database.GetPendingMigrations().ToList();
        }

        public static GameDataImport? ReadGameDataImport(string connectionString)
        {
            var options = new DbContextOptionsBuilder<NosCoreContext>().UseNosCoreDatabase(connectionString).Options;
            using var context = new NosCoreContext(options);
            try
            {
                return context.Set<GameDataImport>().AsNoTracking()
                    .FirstOrDefault(s => s.GameDataImportId == GameDataImportRow);
            }
            catch (DbException)
            {
                return null;
            }
        }

        public static void RecordGameDataImport(DbContext context, string parserBuild, string clientFiles, string folder)
        {
            var set = context.Set<GameDataImport>();
            var row = set.FirstOrDefault(s => s.GameDataImportId == GameDataImportRow);
            if (row == null)
            {
                set.Add(new GameDataImport { GameDataImportId = GameDataImportRow, ParserBuild = parserBuild, ClientFiles = clientFiles, Folder = folder });
            }
            else
            {
                row.ParserBuild = parserBuild;
                row.ClientFiles = clientFiles;
                row.Folder = folder;
            }

            context.SaveChanges();
        }

        public static string? DescribeGameData(GameDataImport? import, string parserBuild)
        {
            if (import == null)
            {
                return "The game data has not been imported. Run the parser, then start the server again.";
            }

            var clientFiles = GameDataFingerprint.TryOfClientFiles(import.Folder);
            var folderSeen = clientFiles != null;
            if (import.ParserBuild == parserBuild && (!folderSeen || import.ClientFiles == clientFiles))
            {
                return null;
            }

            return folderSeen
                ? $"The game data could not be imported again from {import.Folder}. Run the parser to see why, then start the server again."
                : $"The game data was imported by another build of the parser, from {import.Folder}, which this server cannot read. " +
                  "Run the parser again, then start the server again.";
        }

        // False only when the server cannot be reached at all; a missing database still counts,
        // since the parser creates it.
        public static bool CanConnect(string connectionString)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
            {
                return true;
            }
            catch (DbException)
            {
                return false;
            }
        }

        // Held by whoever imports the game data, so that servers starting together import it once.
        // Taken in the maintenance database, since the game database may not exist yet.
        public static IDisposable LockGameDataImport(string connectionString)
        {
            var target = new NpgsqlConnectionStringBuilder(connectionString);
            var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" }.ConnectionString);
            connection.Open();
            using var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key, hashtext(@database))", connection) { CommandTimeout = 0 };
            command.Parameters.AddWithValue("key", GameDataImportLockKey);
            command.Parameters.AddWithValue("database", target.Database ?? string.Empty);
            command.ExecuteNonQuery();
            return connection;
        }

        public static string? Describe(IReadOnlyList<string> pendingMigrations) => pendingMigrations.Count == 0
            ? null
            : $"The database is {pendingMigrations.Count} migration{(pendingMigrations.Count == 1 ? "" : "s")} behind this build " +
              $"(next: {pendingMigrations[0]}). Run the parser to update it, then start the server again.";
    }

    public sealed class DatabaseNotReadyException(string message, Exception? innerException = null)
        : InvalidOperationException(message, innerException);
}
