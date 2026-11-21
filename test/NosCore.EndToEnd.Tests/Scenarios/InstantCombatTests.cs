//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Event;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using EventPacket = NosCore.Data.CommandPackets.EventPacket;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using ServerEventPacket = NosCore.Packets.ServerPackets.Event.EventPacket;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    // An Instant Combat asks everyone whether they join; those who accept are taken to the arena
    // when the sign-up closes, the others are told it went ahead without them.
    [TestClass]
    public class InstantCombatTests
    {
        private const int Channel = 0;
        private static readonly TimeSpan SignUp = TimeSpan.FromSeconds(30);

        // What the client sends when the question is accepted.
        private static readonly GuriPacket Accept = new() { Type = GuriPacketType.EventInWaiting, Argument = -1 };

        private static readonly RequestNpcPacket TalkToTheBroker = new() { Type = VisualType.Npc, TargetId = WorldData.Broker };

        private static async Task<TestClient> AskedToJoinAsync(string name)
        {
            var player = await Harness.AddPlayerAsync(name, authority: AuthorityType.GameMaster);
            var client = Harness.Client(player, channel: Channel);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                var started = client.Mark();
                await client.SendAsync(new EventPacket { EventType = EventType.InstantCombat });
                await client.ExpectSinceAsync<MsgiPacket>(started, m => m.Message == Game18NConstString.InstantCombatStarted,
                    "the combat announced");
                return client;
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }
        }

        [TestMethod]
        public async Task APlayerWhoAcceptsIsTakenToTheArenaWhenTheSignUpCloses()
        {
            await using var client = await AskedToJoinAsync("Joiner");

            await client.SendAsync(Accept);
            // A session's packets are handled in order, so once the broker answers the acceptance is in.
            var talked = client.Mark();
            await client.SendAsync(TalkToTheBroker);
            await client.ExpectSinceAsync<NpcReqPacket>(talked, p => p.TargetId == WorldData.Broker, "the broker's answer");
            var closed = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, SignUp + TimeSpan.FromSeconds(1));

            await client.ExpectSinceAsync<AtPacket>(closed, p => p.MapId == WorldData.InstantCombatArena, "the player in the arena",
                TimeSpan.FromSeconds(20));
        }

        [TestMethod]
        public async Task APlayerWhoDoesNotAnswerStaysWhereTheyAre()
        {
            await using var client = await AskedToJoinAsync("Bystander");

            var closed = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, SignUp + TimeSpan.FromSeconds(1));

            await client.ExpectSinceAsync<EsfPacket>(closed, null, "told the combat went ahead without them", TimeSpan.FromSeconds(20));
            Assert.IsTrue(await client.NoneSinceAsync<AtPacket>(closed, null, TimeSpan.FromSeconds(2)), "the player stays on their map");
        }

        // The fight runs on the channel's clock: the first wave lands 24 s after the arena is entered and
        // the fight ends 15 minutes after that.
        [TestMethod]
        public async Task ASignedUpPlayerFightsTheWavesGetsThePrizeInTheBagAndIsSentBackWhenTimeRunsOut()
        {
            await using var client = await AskedToJoinAsync("Fighter");
            await client.SendAsync(Accept);
            var talked = client.Mark();
            await client.SendAsync(TalkToTheBroker);
            await client.ExpectSinceAsync<NpcReqPacket>(talked, p => p.TargetId == WorldData.Broker, "the broker's answer");
            var closed = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, SignUp + TimeSpan.FromSeconds(1));
            await client.ExpectSinceAsync<AtPacket>(closed, p => p.MapId == WorldData.InstantCombatArena, "the player in the arena",
                TimeSpan.FromSeconds(20));
            var signedUpAt = await client.ExpectSinceAsync<AtPacket>(0, p => p.MapId == WorldData.Town, "where the player signed up");

            var beforeFirstWave = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, TimeSpan.FromSeconds(24));
            await client.ExpectSinceAsync<InPacket>(beforeFirstWave,
                p => p.VisualType == VisualType.Monster && p.VNum == WorldData.InstantCombatWaveVNum.ToString(), "the first wave");
            await client.ExpectSinceAsync<MsgiPacket>(beforeFirstWave, p => p.Message == Game18NConstString.MonsterHaveAppeared);
            Assert.AreEqual(2, client.Since<MsgiPacket>(beforeFirstWave).Count(p => p.Message == Game18NConstString.MonstersAreApproaching),
                "the horde is announced twice before it lands");

            var beforePrize = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, TimeSpan.FromSeconds(130));
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.MonsterWillAppearsInSeconds,
                ArgumentType = 4,
                Game18NArguments = { 40 },
            }, await client.ExpectSinceAsync<MsgiPacket>(beforePrize, p => p.Message == Game18NConstString.MonsterWillAppearsInSeconds));
            await client.ExpectSinceAsync<IvnPacket>(beforePrize, p => p.IvnSubPackets?.Any(i =>
                i.VNum == WorldData.InstantCombatPrize && i.RareAmount == WorldData.InstantCombatPrizeAmount) == true, "the prize in the bag");
            PacketAssert.AreEqual(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = client.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.ReceivedThisItem,
                ArgumentType = 2,
                Game18NArguments = { WorldData.InstantCombatPrize, WorldData.InstantCombatPrizeAmount },
            }, await client.ExpectSinceAsync<SayiPacket>(beforePrize, p => p.Message == Game18NConstString.ReceivedThisItem));

            var beforeEnd = client.Mark();
            await Harness.Stack.AdvanceClockAsync(Channel, TimeSpan.FromSeconds(770));
            await client.ExpectSinceAsync<MsgiPacket>(beforeEnd, p => p.Message == Game18NConstString.InstantCombatThirtySecondsRemaining,
                timeout: TimeSpan.FromSeconds(10));
            CollectionAssert.AreEqual(new long[] { 12, 10, 5, 4, 3, 2, 1 }, client.Since<MsgiPacket>(beforePrize)
                .Where(p => p.Message == Game18NConstString.BattleEndInMinutes)
                .Select(p => Convert.ToInt64(p.Game18NArguments.First())).ToArray(), "the minutes left");
            PacketAssert.AreEqual(new ServerEventPacket { Type = EventPacketType.ClearTimeSpaceUI, Parameter = -1, SecondParameter = -1 },
                await client.ExpectSinceAsync<ServerEventPacket>(beforeEnd, p => p.Type == EventPacketType.ClearTimeSpaceUI));
            var back = await client.ExpectSinceAsync<AtPacket>(beforeEnd, p => p.MapId == WorldData.Town, "back in town");
            Assert.AreEqual((signedUpAt.PositionX, signedUpAt.PositionY), (back.PositionX, back.PositionY), "where the player signed up");
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.InstantCombatOver,
                ArgumentType = 11,
                Game18NArguments = { (int)Game18NConstString.TimeLimitExceeded },
            }, await client.ExpectSinceAsync<MsgiPacket>(beforeEnd, p => p.Message == Game18NConstString.InstantCombatOver));
        }
    }
}
