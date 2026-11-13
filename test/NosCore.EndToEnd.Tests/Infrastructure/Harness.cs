//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // One database and one set of servers for the whole test run. Starting them takes longer
    // than every scenario together, so scenarios share them and keep to their own characters.
    [TestClass]
    public static class Harness
    {
        private static TestDatabase? _database;
        private static ServerStack? _stack;
        private static string? _unavailable;
        private static int _players;

        // The running servers. A test that needs them and cannot have them is reported as not
        // run rather than failed: without a PostgreSQL to reach there is nothing to test.
        public static ServerStack Stack
        {
            get
            {
                if (_stack == null)
                {
                    Assert.Inconclusive(_unavailable ?? "The servers were not started.");
                }

                return _stack;
            }
        }

        public static TestDatabase Database =>
            _database ?? throw new InvalidOperationException(_unavailable ?? "The database was not created.");

        // A new character on an account of its own, standing where the scenario wants it.
        public static async Task<Player> AddPlayerAsync(string name, short mapId = WorldData.Town, short x = 80, short y = 116,
            Action<Character>? configure = null)
        {
            _ = Stack;
            await using var context = Database.CreateContext();
            var unique = name + (char)('a' + Interlocked.Increment(ref _players) % 26) + Random.Shared.Next(100, 999);
            return await WorldData.AddPlayerAsync(context, unique, mapId, x, y, configure);
        }

        public static TestClient Client(Player player, string password = WorldData.Password, int channel = 0) =>
            new(Stack.Host, Stack.LoginPort, player.Account, password, channel);

        // A new character, already in the world.
        public static async Task<TestClient> EnterAsync(string name, short mapId = WorldData.Town, short x = 80, short y = 116,
            Action<Character>? configure = null, int channel = 0)
        {
            var client = Client(await AddPlayerAsync(name, mapId, x, y, configure), channel: channel);
            try
            {
                await client.EnterWorldAsync(0);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        // Puts items in a character's bag. Call it before the character enters the world.
        public static async Task GiveAsync(Player player, short vnum, short amount, short slot = 0)
        {
            await using var context = Database.CreateContext();
            await WorldData.GiveAsync(context, player.CharacterId, vnum, amount, slot);
        }

        // The stored row of a character, as it is now.
        public static async Task<Character> StoredAsync(long characterId)
        {
            await using var context = Database.CreateContext();
            return await context.Set<Character>().AsNoTracking().SingleAsync(c => c.CharacterId == characterId);
        }

        [AssemblyInitialize]
        public static async Task StartAsync(TestContext _)
        {
            _database = await TestDatabase.TryCreateAsync();
            if (_database == null)
            {
                _unavailable = "No PostgreSQL answered on DB_HOST (default localhost) with DB_USER and DB_PASSWORD; " +
                    "the end-to-end scenarios need one.";
                if (string.Equals(Environment.GetEnvironmentVariable("NOSCORE_E2E_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(_unavailable);
                }

                return;
            }

            await using (var context = _database.CreateContext())
            {
                await WorldData.SeedAsync(context);
            }

            _stack = await ServerStack.StartAsync(_database.Name);
        }

        [AssemblyCleanup]
        public static async Task StopAsync()
        {
            if (_stack != null)
            {
                await _stack.DisposeAsync();
            }

            if (_database != null)
            {
                await _database.DisposeAsync();
            }
        }
    }
}
