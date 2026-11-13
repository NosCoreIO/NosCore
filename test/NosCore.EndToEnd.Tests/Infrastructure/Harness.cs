//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
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
        private static int _families;

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
            var unique = $"{name}{Interlocked.Increment(ref _players)}";
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

        // A character that has just left, in the world again: its account is refused until the
        // servers have let go of the session it had.
        public static async Task<TestClient> ComeBackAsync(Player player, int channel = 0)
        {
            var waited = Stopwatch.StartNew();
            while (true)
            {
                var client = Client(player, channel: channel);
                try
                {
                    await client.EnterWorldAsync(player.Slot);
                    return client;
                }
                catch (Exception refused) when (refused is InvalidOperationException or TimeoutException
                    && waited.Elapsed < TimeSpan.FromSeconds(15))
                {
                    await client.DisposeAsync();
                    await Task.Delay(250);
                }
                catch
                {
                    await client.DisposeAsync();
                    throw;
                }
            }
        }

        // Puts items in a character's bag. Call it before the character enters the world.
        public static async Task GiveAsync(Player player, short vnum, short amount, short slot = 0,
            NoscorePocketType pocket = NoscorePocketType.Etc)
        {
            await using var context = Database.CreateContext();
            await WorldData.GiveAsync(context, player.CharacterId, vnum, amount, slot, pocket);
        }

        // The same for one item with properties of its own, such as an upgraded weapon.
        public static async Task GiveAsync(Player player, ItemInstance item, NoscorePocketType pocket, short slot = 0)
        {
            await using var context = Database.CreateContext();
            await WorldData.GiveAsync(context, player.CharacterId, item, slot, pocket);
        }

        // Teaches a character a skill. Call it before the character enters the world.
        public static async Task TeachAsync(Player player, short skillVNum)
        {
            await using var context = Database.CreateContext();
            await WorldData.TeachAsync(context, player.CharacterId, skillVNum);
        }

        // A family name no other scenario of the run uses.
        public static string FamilyName() =>
            $"Clan{Interlocked.Increment(ref _families)}x{Random.Shared.Next(100, 999)}";

        // A family that exists before its members enter the world. The first player is its head.
        public static Task<FoundedFamily> AddFamilyAsync(Player head, params Player[] members) =>
            AddFamilyAsync(null, head, members);

        public static async Task<FoundedFamily> AddFamilyAsync(Action<Family>? configure, Player head, params Player[] members)
        {
            await using var context = Database.CreateContext();
            return await WorldData.AddFamilyAsync(context, FamilyName(), configure, head, members);
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
            var stack = _stack;
            TestClient.ServerDiagnosis = stack.Diagnose;
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
