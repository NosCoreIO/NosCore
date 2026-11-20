//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    // Glacernon runs on a world of its own: the Act 4 channel. It is not in the channel list, a
    // character left in Glacernon is handed on to it, and the ship from town sails there.
    [TestClass]
    public class Act4Tests
    {
        private const int First = 0;
        private const int Second = 1;
        private const int Act4ChannelNumber = 51;
        private const long Fare = 50000;
        private const short CitadelX = 12;
        private const short CitadelY = 38;

        // Boarding opens 25 minutes before each sailing on the hour and half past.
        private static readonly TimeSpan BoardingOpensAfterTheHalfHour = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BoardingToSailing = TimeSpan.FromMinutes(25);

        private static readonly RequestNpcPacket TalkToTheShipmaster = new() { Type = VisualType.Npc, TargetId = WorldData.Shipmaster };

        private static readonly NrunPacket BoardTheShip = new()
        {
            Runner = NrunRunnerType.GoToShipGlacernon, Type = 0, VisualType = VisualType.Npc, VisualId = WorldData.Shipmaster
        };

        [TestMethod]
        public async Task ACharacterLeftInGlacernonIsHandedOnToTheAct4Channel()
        {
            var player = await InGlacernonAsync("Alice");
            await using var client = Harness.Client(player);

            await client.StartGameAsync(player.Slot);

            var handover = await client.ExpectAsync<MzPacket>();
            Assert.AreEqual(Harness.Stack.Act4Port, handover.Port);
            PacketAssert.AreEqual(new ItPacket { Mode = ItModeType.ToAct4 }, await client.ExpectAsync<ItPacket>());
            var arrival = await client.FollowAsync(handover);
            Assert.IsTrue(arrival.OfType<CMapPacket>().Any(map => map.Id == WorldData.AngelCitadel), "the character is in its citadel");
        }

        [TestMethod]
        public async Task AWhisperFromGlacernonNamesTheAct4Channel()
        {
            var alicePlayer = await InGlacernonAsync("Alice");
            await using var alice = Harness.Client(alicePlayer);
            await alice.StartGameAsync(alicePlayer.Slot);
            await alice.FollowAsync(await alice.ExpectAsync<MzPacket>());
            var bobPlayer = await Harness.AddPlayerAsync("Bob");
            await using var bob = Harness.Client(bobPlayer, channel: First);
            await bob.EnterWorldAsync(bobPlayer.Slot);

            await alice.SendAsync(new WhisperPacket { Message = $"{bobPlayer.Name} greetings from the citadel" });

            var heard = await bob.ExpectAsync<SpeakPacket>(p => p.VisualId == alice.CharacterId);
            StringAssert.Contains(heard.Message, "greetings from the citadel");
            StringAssert.EndsWith(heard.Message, $": {Act4ChannelNumber}>");
        }

        [TestMethod]
        public async Task TheShipTakesAPassengerToGlacernonForTheFare()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: WorldData.ShipmasterX - 1, y: WorldData.ShipmasterY,
                configure: c =>
                {
                    c.Faction = FactionType.Angel;
                    c.Gold = Fare + 10000;
                });
            await using var client = Harness.Client(player, channel: Second);
            await client.EnterWorldAsync(player.Slot);
            await BoardingOpensAsync();

            await BoardAsync(client);
            await Harness.Stack.AdvanceClockAsync(Second, BoardingToSailing);

            var sailed = await client.ExpectAsync<MzPacket>(timeout: TimeSpan.FromSeconds(20));
            Assert.AreEqual(Harness.Stack.Act4Port, sailed.Port);
            PacketAssert.AreEqual(new ItPacket { Mode = ItModeType.ShipToAct4 }, await client.ExpectAsync<ItPacket>());
            var arrival = await client.FollowAsync(sailed);
            Assert.IsTrue(arrival.OfType<CMapPacket>().Any(map => map.Id == WorldData.AngelCitadel), "the ship lands at the angels' citadel");
            Assert.AreEqual(10000, (await Harness.StoredAsync(player.CharacterId)).Gold, "the fare is taken as the ship sails");
        }

        private static Task<Player> InGlacernonAsync(string name) =>
            Harness.AddPlayerAsync(name, WorldData.AngelCitadel, CitadelX, CitadelY, c => c.Faction = FactionType.Angel);

        // Takes the second channel's clock to just after boarding next opens. The schedule starts
        // one event at a time and skips a start the clock has already passed, so the jump stops
        // short first and lets every event it carried past start, and a boarding window already
        // open close, before the last step.
        private static async Task BoardingOpensAsync()
        {
            var now = await Harness.Stack.AdvanceClockAsync(Second, TimeSpan.Zero);
            var local = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local);
            var opens = new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, 0, 0, local.Offset) + BoardingOpensAfterTheHalfHour;
            while (opens <= local)
            {
                opens += TimeSpan.FromMinutes(30);
            }

            // The server ignores a jump backwards, so with boarding opening in under 30 s the first jump stays put.
            var firstJump = opens - local - TimeSpan.FromSeconds(30);
            var stopped = await Harness.Stack.AdvanceClockAsync(Second, firstJump > TimeSpan.Zero ? firstJump : TimeSpan.Zero);
            await Task.Delay(TimeSpan.FromSeconds(4));
            await Harness.Stack.AdvanceClockAsync(Second, opens - stopped + TimeSpan.FromSeconds(5));
        }

        // The schedule opens boarding on the channel's own timer, so the first request may still
        // find the ship away.
        private static async Task BoardAsync(TestClient client)
        {
            var waited = Stopwatch.StartNew();
            while (true)
            {
                await client.SendAsync(TalkToTheShipmaster);
                await client.ExpectAsync<NpcReqPacket>(p => p.TargetId == WorldData.Shipmaster);
                await client.SendAsync(BoardTheShip);
                if (await client.WaitAsync<CMapPacket>(p => p.Id == WorldData.ShipDeck, TimeSpan.FromSeconds(2)) != null)
                {
                    return;
                }

                if (waited.Elapsed > TimeSpan.FromSeconds(10))
                {
                    Assert.Fail("Boarding did not open on the second channel.");
                }
            }
        }
    }
}
