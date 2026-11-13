//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ParcelTests
    {
        private const int EtcPocket = 2;
        private const int Received = 1;
        private const int Sent = 2;

        // pst <1: send> <box> <id> -1 0 <receiver> <title> <text>
        private static string Note(string receiver, string title, string text) => $"pst 1 1 0 -1 0 {receiver} {title} {text}";

        private static async Task<TestClient> EnterAsync(Player player)
        {
            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }

        private static async Task<Player> GameMasterAsync()
        {
            var player = await Harness.AddPlayerAsync("Gm");
            await using var context = Harness.Database.CreateContext();
            var account = await context.Set<Account>().SingleAsync(a => a.Name == player.Account);
            account.Authority = AuthorityType.GameMaster;
            await context.SaveChangesAsync();
            return player;
        }

        private static async Task<Mail[]> StoredMailAsync(long receiverId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<Mail>().AsNoTracking().Where(m => m.ReceiverId == receiverId).ToArrayAsync();
        }

        private static async Task<int> StoredSeedsAsync(long characterId)
        {
            await using var context = Harness.Database.CreateContext();
            return await context.Set<InventoryItemInstance>().AsNoTracking()
                .Where(i => i.CharacterId == characterId && i.ItemInstance.ItemVNum == WorldData.Seed)
                .SumAsync(i => (int)i.ItemInstance.Amount);
        }

        // parcel 1 <1: received> <id> <title type> 0 <date> <title> <vnum> <amount> <item type>
        private static async Task<string> GiftAsync(TestClient gm, TestClient receiver, Player to, int amount)
        {
            await gm.SendAsync($"$Gift {WorldData.Seed} {amount} 0 0 {to.Name}");
            var parcel = await receiver.ExpectAsync(p => p.StartsWith($"parcel 1 {Received} ", StringComparison.Ordinal), "the parcel");
            var fields = parcel.Split(' ');
            Assert.AreEqual($"{WorldData.Seed} {amount}", $"{fields[^3]} {fields[^2]}", parcel);
            return fields[3];
        }

        [TestMethod]
        public async Task ANoteToNobodySaysSo()
        {
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync(Note("Nobody", "Lunch", "noon"));

            // 810 is the client's notice that the player cannot be found.
            await alice.ExpectAsync(p => p.StartsWith($"sayi 1 {alice.CharacterId} 10 810 ", StringComparison.Ordinal), "the not-found notice");
        }

        [TestMethod]
        public async Task AnItemTakenFromAParcelIsInTheBagAfterComingBack()
        {
            var master = await GameMasterAsync();
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var gm = await EnterAsync(master);
            await using (var bob = await EnterAsync(receiver))
            {
                var id = await GiftAsync(gm, bob, receiver, 3);
                await bob.SendAsync($"pcl 4 {id}");
                await bob.ExpectAsync(p => p == $"parcel 2 1 {id}", "the parcel taken");
            }

            var waited = Stopwatch.StartNew();
            while (await StoredSeedsAsync(receiver.CharacterId) != 3)
            {
                Assert.IsTrue(waited.Elapsed < TimeSpan.FromSeconds(15), "the three were not stored in the bag within 15 s of leaving");
                await Task.Delay(250);
            }

            await using var back = await Harness.ComeBackAsync(receiver);
            Assert.IsTrue(back.Since(0).Any(p => p.StartsWith($"inv {EtcPocket} 0.{WorldData.Seed}.3.", StringComparison.Ordinal)),
                "the three are in the bag");
            Assert.IsTrue(await back.NoneSinceAsync(0, p => p.StartsWith($"parcel 1 {Received} ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "and the parcel is gone");
        }

        [TestMethod]
        public async Task AParcelIsTakenOnlyOnce()
        {
            var master = await GameMasterAsync();
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var gm = await EnterAsync(master);
            await using var bob = await EnterAsync(receiver);
            var id = await GiftAsync(gm, bob, receiver, 3);
            await bob.SendAsync($"pcl 4 {id}");
            await bob.ExpectAsync(p => p == $"parcel 2 1 {id}", "the parcel taken");
            var again = bob.Mark();

            await bob.SendAsync($"pcl 4 {id}");

            Assert.IsTrue(await bob.NoneSinceAsync(again, p => TestClient.HeaderOf(p) is "parcel" or "ivn", TimeSpan.FromSeconds(1)));
        }

        [TestMethod]
        public async Task AParcelCanBeThrownAway()
        {
            var master = await GameMasterAsync();
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var gm = await EnterAsync(master);
            await using var bob = await EnterAsync(receiver);
            var id = await GiftAsync(gm, bob, receiver, 3);
            var thrown = bob.Mark();

            await bob.SendAsync($"pcl 5 {id}");

            await bob.ExpectSinceAsync(thrown, p => p == $"parcel 7 1 {id}", "the parcel removed");
            Assert.IsFalse((await StoredMailAsync(receiver.CharacterId)).Any(m => !m.IsSenderCopy), "the parcel is no longer stored");
            Assert.IsTrue(await bob.NoneSinceAsync(thrown, p => TestClient.HeaderOf(p) == "ivn", TimeSpan.FromSeconds(1)), "nothing goes to the bag");
        }

        [TestMethod]
        public async Task OnlyAGameMasterSendsGifts()
        {
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice");
            await using var bob = await EnterAsync(receiver);
            var sent = bob.Mark();

            await alice.SendAsync($"$Gift {WorldData.Seed} 3 0 0 {receiver.Name}");

            Assert.IsTrue(await bob.NoneSinceAsync(sent, p => TestClient.HeaderOf(p) == "parcel", TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredMailAsync(receiver.CharacterId)).Length);
        }
    }
}
