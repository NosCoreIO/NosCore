//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Event;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using EventPacket = NosCore.Data.CommandPackets.EventPacket;
using GuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

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
    }
}
