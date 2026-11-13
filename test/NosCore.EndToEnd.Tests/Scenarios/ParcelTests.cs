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
        public async Task ANoteReachesItsReceiverAndCanBeRead()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);
            await using var bob = await EnterAsync(receiver);

            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));

            // 813 is the client's notice that the note was sent.
            await alice.ExpectAsync(p => p.StartsWith($"sayi 1 {alice.CharacterId} 11 813 ", StringComparison.Ordinal), "the sent notice");
            Assert.AreEqual("noon", (await StoredMailAsync(receiver.CharacterId)).Single(m => !m.IsSenderCopy).Message, "the note is stored");
            // post 1 <1: received> <id> 0 <opened> <date> <sender> <title> <text>
            var note = (await bob.ExpectAsync(p => p.StartsWith($"post 1 {Received} ", StringComparison.Ordinal), "the note")).Split(' ');
            Assert.AreEqual($"{sender.Name} Lunch noon", string.Join(' ', note.TakeLast(3)));
            Assert.AreEqual("0", note[5], "it is unread");

            await bob.SendAsync($"pst 3 {Received} {note[3]}");

            var opened = await bob.ExpectAsync(p => p.StartsWith($"post 5 {Received} {note[3]} ", StringComparison.Ordinal), "the opened note");
            StringAssert.EndsWith(opened, $" {sender.Name} Lunch noon");
            var stored = (await StoredMailAsync(receiver.CharacterId)).Single(m => !m.IsSenderCopy);
            Assert.AreEqual("noon", stored.Message);
            Assert.IsTrue(stored.IsOpened, "a note that was read is stored as opened");
        }

        [TestMethod]
        public async Task ANoteWaitsForAReceiverWhoIsAway()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);

            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));
            await alice.ExpectAsync(p => p.StartsWith($"sayi 1 {alice.CharacterId} 11 813 ", StringComparison.Ordinal), "the sent notice");

            await using var bob = Harness.Client(receiver);
            await bob.EnterWorldAsync(receiver.Slot);
            var note = await bob.ExpectSinceAsync(0, p => p.StartsWith($"post 1 {Received} ", StringComparison.Ordinal), "the note");
            StringAssert.EndsWith(note, $" {sender.Name} Lunch noon");
        }

        [TestMethod]
        public async Task TheSenderKeepsACopyOfANote()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);
            await using var bob = await EnterAsync(receiver);
            var sent = bob.Mark();

            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));

            // A sent note names who it went to.
            var copy = await alice.ExpectAsync(p => p.StartsWith($"post 1 {Sent} ", StringComparison.Ordinal), "the sender's copy");
            StringAssert.EndsWith(copy, $" {receiver.Name} Lunch noon");
            await bob.ExpectSinceAsync(sent, p => p.StartsWith($"post 1 {Received} ", StringComparison.Ordinal), "the note");
            Assert.IsTrue(await bob.NoneSinceAsync(sent, p => p.StartsWith($"post 1 {Sent} ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "the receiver is not given the sender's copy");
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
        public async Task ADeletedNoteIsGone()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);
            await using var bob = await EnterAsync(receiver);
            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));
            var note = (await bob.ExpectAsync(p => p.StartsWith($"post 1 {Received} ", StringComparison.Ordinal), "the note")).Split(' ');

            await bob.SendAsync($"pst 2 {Received} {note[3]}");

            // 814 is the client's notice that the note was deleted.
            await bob.ExpectAsync(p => p.StartsWith($"sayi 1 {bob.CharacterId} 11 814 ", StringComparison.Ordinal), "the deleted notice");
            Assert.IsFalse((await StoredMailAsync(receiver.CharacterId)).Any(m => !m.IsSenderCopy), "the note is no longer stored");
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
        public async Task TheReceiverOfAGiftGetsOneParcel()
        {
            var master = await GameMasterAsync();
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var gm = await EnterAsync(master);
            await using var bob = await EnterAsync(receiver);
            var sent = bob.Mark();

            await GiftAsync(gm, bob, receiver, 3);

            Assert.IsTrue(await bob.NoneSinceAsync(sent, p => p.StartsWith($"parcel 1 {Sent} ", StringComparison.Ordinal), TimeSpan.FromSeconds(1)),
                "the receiver is not given the sender's copy");
            // The list sent on entering can carry the same parcel a second time; it is still one parcel.
            Assert.AreEqual(1, bob.Since(sent).Where(p => p.StartsWith("parcel 1 ", StringComparison.Ordinal)).Distinct().Count());
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
