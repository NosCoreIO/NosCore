//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class TimeSpaceClockTests
    {
        // The channel whose clock this scenario moves, as the Act 4 scenarios do.
        private const int OwnClock = 1;
        private const short WayOutX = 30;
        private const short WayOutY = 33;
        // A diagonal step of five cells is past what one walk may cover, and a refused walk strands the
        // character where the server last had it.
        private const int Stride = 4;
        private static readonly TimeSpan StrideTime = TimeSpan.FromMilliseconds(1300);

        [TestMethod]
        public async Task AClockThatRunsOutFailsTheTimeSpaceAsATimeout()
        {
            var player = await Harness.AddPlayerAsync("Tim", WorldData.Meadow, WorldData.SampleTimeSpaceX,
                WorldData.SampleTimeSpaceY + 1);
            await using var tim = Harness.Client(player, channel: OwnClock);
            await tim.EnterWorldAsync(player.Slot);
            var entering = tim.Mark();

            await tim.SendAsync(new TreqPacket
            {
                X = WorldData.SampleTimeSpaceX, Y = WorldData.SampleTimeSpaceY, StartPress = true
            });
            var arrived = await tim.ExpectSinceAsync<AtPacket>(entering, p => p.CharacterId == tim.CharacterId,
                "the character in the first room");
            var waiting = await tim.ExpectSinceAsync<EventPacket>(entering, p => p.Type == EventPacketType.TSTimer,
                "the lobby's clock");
            PacketAssert.AreEqual(new EventPacket
            {
                Type = EventPacketType.TSTimer, State = EventTimerState.Waiting,
                Parameter = waiting.SecondParameter, SecondParameter = waiting.SecondParameter
            }, waiting);
            var monsters = new List<InPacket>();
            while (monsters.Count < 3)
            {
                var seen = monsters.Select(m => m.VisualId).ToHashSet();
                monsters.Add(await tim.ExpectSinceAsync<InPacket>(entering,
                    p => p.VisualType == VisualType.Monster && !seen.Contains(p.VisualId), "a monster of the first room"));
            }

            var (x, y) = (arrived.PositionX, arrived.PositionY);
            foreach (var monster in monsters)
            {
                var (nextX, nextY) = ((short)(monster.PositionX - 1), monster.PositionY);
                await WalkToAsync(tim, x, y, nextX, nextY);
                (x, y) = (nextX, nextY);
                await Steps.KillAsync(tim, monster.VisualId);
            }

            await WalkToAsync(tim, x, y, WayOutX - 1, WayOutY);
            var asking = tim.Mark();
            await tim.SendAsync(new PreqPacket());
            var question = await tim.ExpectSinceAsync<DlgiPacket>(asking, description: "the question whether to start");
            Assert.IsTrue(question.NoPacket is RStartPacket { IsWrittenWithoutHash: true }, "No answers with a bare rstart");
            var starting = tim.Mark();
            await tim.SendAsync(new RStartPacket { Type = RStartPacketType.TimeSpaceInstanceEntry });
            await tim.ExpectSinceAsync<EventPacket>(starting,
                p => p.Type == EventPacketType.TSTimer && p.State == EventTimerState.Running, "the clock started");

            var running = tim.Mark();
            await Harness.Stack.AdvanceClockAsync(OwnClock, TimeSpan.FromMinutes(11));

            var failed = await tim.ExpectSinceAsync<ScoreFailedPacket>(running, description: "the run failing on its clock",
                timeout: TimeSpan.FromSeconds(20));
            PacketAssert.AreEqual(new ScoreFailedPacket { EndState = EndStateType.Timeout }, failed);
        }

        private static async Task WalkToAsync(TestClient client, short fromX, short fromY, short x, short y)
        {
            while (fromX != x || fromY != y)
            {
                fromX = (short)(fromX + Math.Clamp(x - fromX, -Stride, Stride));
                fromY = (short)(fromY + Math.Clamp(y - fromY, -Stride, Stride));
                await client.WalkAsync(fromX, fromY);
                await Task.Delay(StrideTime);
            }
        }
    }
}
