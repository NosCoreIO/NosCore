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
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class MapTests
    {
        // A portal cannot be taken in the four seconds after arriving on a map.
        private static readonly TimeSpan PortalRest = TimeSpan.FromSeconds(4.2);

        [TestMethod]
        public async Task APlayerWhoArrivesIsShownToThoseAlreadyThere()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            var alice = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using var aliceClient = Harness.Client(alice);

            await aliceClient.EnterWorldAsync(alice.Slot);

            var arrival = await bob.ExpectAsync<InPacket>(p => p.VisualType == VisualType.Player && p.Name == alice.Name, "in for Alice");
            Assert.AreEqual((alice.CharacterId, (short)80, (short)116), (arrival.VisualId, arrival.PositionX, arrival.PositionY));
            await aliceClient.ExpectSinceAsync<InPacket>(0, p => p.VisualType == VisualType.Player && p.VisualId == bob.CharacterId,
                "in for Bob, who was already there");
        }

        [TestMethod]
        public async Task AStepIsSeenByTheOtherPlayer()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

            await alice.WalkAsync(81, 117);

            var step = await bob.ExpectAsync<MovePacket>(p => p.VisualType == VisualType.Player && p.VisualEntityId == alice.CharacterId, "mv for Alice");
            PacketAssert.AreEqual(new MovePacket
            {
                VisualType = VisualType.Player,
                VisualEntityId = alice.CharacterId,
                MapX = 81,
                MapY = 117,
                Speed = (byte)alice.Speed,
            }, step);
        }

        [TestMethod]
        public async Task WhatIsSaidIsHeardByTheOthersOnly()
        {
            await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

            var said = alice.Mark();
            await alice.SendAsync(new ClientSayPacket { Message = "hello there" });

            PacketAssert.AreEqual(new SayPacket
            {
                VisualType = VisualType.Player,
                VisualId = alice.CharacterId,
                Type = SayColorType.Default,
                Message = "hello there",
            }, await bob.ExpectAsync<SayPacket>());
            Assert.IsTrue(await alice.NoneSinceAsync<SayPacket>(said, null, TimeSpan.FromSeconds(1)),
                "the speaker's client draws its own line");
        }

        [TestMethod]
        public async Task APortalLeadsToTheOtherMap()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 89, y: 116);
            await Task.Delay(PortalRest);

            await alice.WalkAsync(90, 116);
            await alice.SendAsync(new PreqPacket());

            var at = await alice.ExpectAsync<AtPacket>();
            Assert.AreEqual((alice.CharacterId, (short)WorldData.Meadow, (short)10, (short)10), (at.CharacterId, at.MapId, at.PositionX, at.PositionY));
        }

        [TestMethod]
        public async Task APortalIsRefusedRightAfterArriving()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 90, y: 116);

            var asked = alice.Mark();
            await alice.SendAsync(new PreqPacket());

            await alice.ExpectAsync<SayiPacket>(p => p.VisualId == alice.CharacterId && p.Type == SayColorType.Yellow
                && p.Message == Game18NConstString.WillMoveShortly, "sayi WillMoveShortly");
            Assert.IsTrue(await alice.NoneSinceAsync<AtPacket>(asked, null, TimeSpan.FromSeconds(1)),
                "the character stays on its map");
        }

        [TestMethod]
        public async Task APlayerWhoLeavesIsSavedWhereTheyStood()
        {
            var player = await Harness.AddPlayerAsync("Alice", x: 80, y: 116);
            await using (var client = Harness.Client(player))
            {
                await client.EnterWorldAsync(player.Slot);
                await client.WalkAsync(81, 117);
                await Task.Delay(500);
            }

            await SavedAtAsync(player, 81, 117);

            await using var back = await Harness.ComeBackAsync(player);
            var at = back.Received.OfType<AtPacket>().Single();
            Assert.AreEqual(((short)WorldData.Town, (short)81, (short)117), (at.MapId, at.PositionX, at.PositionY));
        }

        private static async Task SavedAtAsync(Player player, short x, short y)
        {
            var waited = Stopwatch.StartNew();
            while (waited.Elapsed < TimeSpan.FromSeconds(15))
            {
                await using var context = Harness.Database.CreateContext();
                var saved = await context.Set<NosCore.Database.Entities.Character>().AsNoTracking().SingleAsync(c => c.CharacterId == player.CharacterId);
                if (saved.MapX == x && saved.MapY == y)
                {
                    return;
                }

                await Task.Delay(250);
            }

            Assert.Fail($"{player.Name} was not saved at {x},{y} within 15 s of leaving.");
        }
    }
}
