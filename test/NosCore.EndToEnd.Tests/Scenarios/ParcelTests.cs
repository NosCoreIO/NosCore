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
using NosCore.Data.CommandPackets;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ParcelTests
    {
        private static PstClientPacket Note(string receiver, string title, string text) => new()
        {
            ActionType = PostActionType.Send,
            Type = MailBoxType.Sent,
            Id = 0,
            Unknow1 = null,
            Unknow2 = 0,
            ReceiverName = receiver,
            Title = title,
            Text = text,
        };

        private static Func<PostPacket, bool> Listed(MailBoxType box) => packet => packet.Type == PostPacketType.Listed && packet.PostType == box;

        private static Func<ParcelPacket, bool> Listed() => packet => packet.Type == ParcelPacketType.Listed && packet.Box == MailBoxType.Received;

        private static Func<IvnPacket, bool> ThreeSeeds =>
            packet => packet.Type == PocketType.Etc
                && packet.IvnSubPackets?.Single() is { Slot: 0 } item && item.VNum == WorldData.Seed && item.RareAmount == 3;

        private static Func<SayiPacket, bool> Notice(TestClient client, SayColorType color, Game18NConstString message) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == client.CharacterId
                && packet.Type == color && packet.Message == message;

        private static Func<ParcelPacket, bool> Parcel(ParcelPacketType type, short id) =>
            packet => packet.Type == type && packet.Box == MailBoxType.Received && packet.Id == id;

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

        private static async Task<short> GiftAsync(TestClient gm, TestClient receiver, Player to, byte amount)
        {
            await gm.SendAsync(new GiftPacket { VNum = WorldData.Seed, Amount = amount, Rare = 0, Upgrade = 0, CharacterName = to.Name });
            var parcel = await receiver.ExpectAsync(Listed(), "the parcel");
            Assert.AreEqual((WorldData.Seed, (short)amount),
                (parcel.ParcelAttachment?.AttachmentVNum, parcel.ParcelAttachment?.AttachmentAmount));
            return parcel.Id;
        }

        private static PclPacket Take(short id) => new() { Type = 4, GiftId = id };

        [TestMethod]
        public async Task ANoteReachesItsReceiverAndCanBeRead()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);
            await using var bob = await EnterAsync(receiver);

            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));

            await alice.ExpectAsync(Notice(alice, SayColorType.Red, Game18NConstString.NoteSent), "the sent notice");
            Assert.AreEqual("noon", (await StoredMailAsync(receiver.CharacterId)).Single(m => !m.IsSenderCopy).Message, "the note is stored");
            var note = await bob.ExpectAsync(Listed(MailBoxType.Received), "the note");
            Assert.AreEqual((sender.Name, "Lunch", "noon"), (note.SenderName, note.Title, note.Message));
            Assert.IsFalse(note.IsOpened, "it is unread");

            await bob.SendAsync(new PstClientPacket { ActionType = PostActionType.Read, Type = MailBoxType.Received, Id = note.Id });

            var opened = await bob.ExpectAsync<PostPacket>(p => p.Type == PostPacketType.Opened && p.PostType == MailBoxType.Received && p.Id == note.Id, "the opened note");
            Assert.AreEqual((sender.Name, "Lunch", "noon"), (opened.SenderName, opened.Title, opened.Message));
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
            await alice.ExpectAsync(Notice(alice, SayColorType.Red, Game18NConstString.NoteSent), "the sent notice");

            await using var bob = Harness.Client(receiver);
            await bob.EnterWorldAsync(receiver.Slot);
            var note = await bob.ExpectSinceAsync(0, Listed(MailBoxType.Received), "the note");
            Assert.AreEqual((sender.Name, "Lunch", "noon"), (note.SenderName, note.Title, note.Message));
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
            var copy = await alice.ExpectAsync(Listed(MailBoxType.Sent), "the sender's copy");
            Assert.AreEqual((receiver.Name, "Lunch", "noon"), (copy.SenderName, copy.Title, copy.Message));
            await bob.ExpectSinceAsync(sent, Listed(MailBoxType.Received), "the note");
            Assert.IsTrue(await bob.NoneSinceAsync(sent, Listed(MailBoxType.Sent), TimeSpan.FromSeconds(1)),
                "the receiver is not given the sender's copy");
        }

        [TestMethod]
        public async Task ANoteToNobodySaysSo()
        {
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync(Note("Nobody", "Lunch", "noon"));

            await alice.ExpectAsync(Notice(alice, SayColorType.Yellow, Game18NConstString.CanNotFindPlayer), "the not-found notice");
        }

        [TestMethod]
        public async Task ADeletedNoteIsGone()
        {
            var sender = await Harness.AddPlayerAsync("Alice");
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await EnterAsync(sender);
            await using var bob = await EnterAsync(receiver);
            await alice.SendAsync(Note(receiver.Name, "Lunch", "noon"));
            var note = await bob.ExpectAsync(Listed(MailBoxType.Received), "the note");

            await bob.SendAsync(new PstClientPacket { ActionType = PostActionType.Delete, Type = MailBoxType.Received, Id = note.Id });

            await bob.ExpectAsync(Notice(bob, SayColorType.Red, Game18NConstString.NoteDeleted), "the deleted notice");
            Assert.IsFalse((await StoredMailAsync(receiver.CharacterId)).Any(m => !m.IsSenderCopy), "the note is no longer stored");
        }

        [TestMethod]
        public async Task AGiftArrivesAsAParcelAndItsItemIsTakenIntoTheBag()
        {
            var master = await GameMasterAsync();
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var gm = await EnterAsync(master);
            await using var bob = await EnterAsync(receiver);

            var id = await GiftAsync(gm, bob, receiver, 3);
            var taken = bob.Mark();
            await bob.SendAsync(Take(id));

            await bob.ExpectSinceAsync(taken, Parcel(ParcelPacketType.Received, id), "the parcel taken");
            Assert.IsFalse((await StoredMailAsync(receiver.CharacterId)).Any(m => !m.IsSenderCopy), "the parcel is no longer stored");
            await bob.ExpectSinceAsync(taken, ThreeSeeds, "the three in the bag");
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
                await bob.SendAsync(Take(id));
                await bob.ExpectAsync(Parcel(ParcelPacketType.Received, id), "the parcel taken");
            }

            var waited = Stopwatch.StartNew();
            while (await StoredSeedsAsync(receiver.CharacterId) != 3)
            {
                Assert.IsTrue(waited.Elapsed < TimeSpan.FromSeconds(15), "the three were not stored in the bag within 15 s of leaving");
                await Task.Delay(250);
            }

            await using var back = await Harness.ComeBackAsync(receiver);
            Assert.IsTrue(back.Received.OfType<InvPacket>().Where(p => p.Type == PocketType.Etc).SelectMany(p => p.IvnSubPackets ?? [])
                    .Any(item => item.Slot == 0 && item.VNum == WorldData.Seed && item.RareAmount == 3),
                "the three are in the bag");
            Assert.IsTrue(await back.NoneSinceAsync(0, Listed(), TimeSpan.FromSeconds(1)),
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
            await bob.SendAsync(Take(id));
            await bob.ExpectAsync(Parcel(ParcelPacketType.Received, id), "the parcel taken");
            var again = bob.Mark();

            await bob.SendAsync(Take(id));

            Assert.IsTrue(await bob.NoneSinceAsync<ParcelPacket>(again, null, TimeSpan.FromSeconds(1)));
            Assert.IsTrue(await bob.NoneSinceAsync<IvnPacket>(again, null, TimeSpan.Zero));
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

            await bob.SendAsync(new PclPacket { Type = 5, GiftId = id });

            await bob.ExpectSinceAsync(thrown, Parcel(ParcelPacketType.Deleted, id), "the parcel removed");
            Assert.IsFalse((await StoredMailAsync(receiver.CharacterId)).Any(m => !m.IsSenderCopy), "the parcel is no longer stored");
            Assert.IsTrue(await bob.NoneSinceAsync<IvnPacket>(thrown, null, TimeSpan.FromSeconds(1)), "nothing goes to the bag");
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

            Assert.IsTrue(await bob.NoneSinceAsync<ParcelPacket>(sent, p => p.Type == ParcelPacketType.Listed && p.Box == MailBoxType.Sent, TimeSpan.FromSeconds(1)),
                "the receiver is not given the sender's copy");
            // The list sent on entering can carry the same parcel a second time; it is still one parcel.
            Assert.AreEqual(1, bob.Since<ParcelPacket>(sent).Where(p => p.Type == ParcelPacketType.Listed).Select(p => p.Id).Distinct().Count());
        }

        [TestMethod]
        public async Task OnlyAGameMasterSendsGifts()
        {
            var receiver = await Harness.AddPlayerAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice");
            await using var bob = await EnterAsync(receiver);
            var sent = bob.Mark();

            await alice.SendAsync(new GiftPacket { VNum = WorldData.Seed, Amount = 3, Rare = 0, Upgrade = 0, CharacterName = receiver.Name });

            Assert.IsTrue(await bob.NoneSinceAsync<ParcelPacket>(sent, null, TimeSpan.FromSeconds(1)));
            Assert.AreEqual(0, (await StoredMailAsync(receiver.CharacterId)).Length);
        }
    }
}
