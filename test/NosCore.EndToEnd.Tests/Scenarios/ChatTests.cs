//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ChatTests
    {
        [TestMethod]
        public async Task AWhisperReachesThePlayerItNames()
        {
            var bobPlayer = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 20, 20);
            await using var bob = Harness.Client(bobPlayer);
            await bob.EnterWorldAsync(bobPlayer.Slot);
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync(new WhisperPacket { Message = $"{bobPlayer.Name} see you at the portal" });

            var heard = await bob.ExpectAsync<SpeakPacket>();
            Assert.AreEqual((VisualType.Player, alice.CharacterId), (heard.VisualType, heard.VisualId));
            StringAssert.Contains(heard.Message, "see you at the portal");
        }

        [TestMethod]
        public async Task AWhisperReachesSomeoneWhoHasJustArrived()
        {
            await using var alice = await Harness.EnterAsync("Alice");
            var bobPlayer = await Harness.AddPlayerAsync("Bob", WorldData.Meadow, 20, 20);
            await using var bob = Harness.Client(bobPlayer);

            // The first whisper makes the channel read who is online; Bob arrives after that.
            await alice.SendAsync(new WhisperPacket { Message = $"{bobPlayer.Name} are you there yet" });
            await alice.ExpectAsync<Infoi2Packet>();
            await bob.EnterWorldAsync(bobPlayer.Slot);
            await alice.SendAsync(new WhisperPacket { Message = $"{bobPlayer.Name} there you are" });

            var heard = await bob.ExpectAsync<SpeakPacket>();
            StringAssert.Contains(heard.Message, "there you are");
        }

        [TestMethod]
        public async Task AWhisperToNobodySaysSo()
        {
            await using var alice = await Harness.EnterAsync("Alice");

            await alice.SendAsync(new WhisperPacket { Message = "Nobody are you there" });

            await alice.ExpectAsync<Infoi2Packet>(p => p.Message == Game18NConstString.IsNotPlaying && p.Game18NArguments.Contains("Nobody"),
                "the not-playing notice");
        }

        [TestMethod]
        public async Task AMutedAccountsWordsReachNobody()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using (var context = Harness.Database.CreateContext())
            {
                var account = await Harness.StoredAsync(player.CharacterId);
                var now = SystemClock.Instance.GetCurrentInstant();
                context.Add(new PenaltyLog
                {
                    AccountId = account.AccountId, Penalty = PenaltyType.Muted, AdminName = "admin", Reason = "test",
                    DateStart = now, DateEnd = now.Plus(Duration.FromMinutes(30))
                });
                await context.SaveChangesAsync();
            }

            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);

            var said = bob.Mark();
            await alice.SendAsync(new ClientSayPacket { Message = "hello" });

            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = alice.CharacterId,
                Type = SayColorType.BubbleOnly,
                Message = Game18NConstString.IAmUnderPenalty,
            }, await bob.ExpectAsync<SayiPacket>(p => p.Message == Game18NConstString.IAmUnderPenalty, "the penalty bubble"));
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = alice.CharacterId,
                Type = SayColorType.Red,
                Message = Game18NConstString.RemainingPunishment,
                ArgumentType = 4,
                Game18NArguments = { 30 },
            }, await alice.ExpectAsync<SayiPacket>(p => p.Message == Game18NConstString.RemainingPunishment, "the minutes left"));
            Assert.IsTrue(await bob.NoneSinceAsync<SayPacket>(said, null, TimeSpan.FromSeconds(1)),
                "the words themselves are sent to nobody");
        }
    }
}
