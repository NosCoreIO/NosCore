//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class BoxTests
    {
        private static Func<IvnPacket, bool> Slot(PocketType pocket, short slot, short vnum, short amount) =>
            packet => packet.Type == pocket
                && packet.IvnSubPackets?.Any(item => item.Slot == slot && item.VNum == vnum && item.RareAmount == amount) == true;

        private static Func<IvnPacket, bool> EmptySlot(PocketType pocket, short slot) =>
            packet => packet.Type == pocket && packet.IvnSubPackets?.Single() is { } item && item.Slot == slot && item.VNum is 0 or -1;

        private static async Task<TestClient> HoldingAsync(short box)
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, box, 1, pocket: NoscorePocketType.Main);
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

        private static Task OpenAsync(TestClient client) => client.SendAsync(new UseItemPacket
        {
            VisualType = VisualType.Player, VisualId = client.CharacterId, Type = PocketType.Main, Slot = 0, Mode = 0, Parameter = 0
        });

        [TestMethod]
        public async Task OpeningABoxPutsWhatItHoldsInTheBagAndUsesItUp()
        {
            await using var alice = await HoldingAsync(WorldData.Box);

            var opened = alice.Mark();
            await OpenAsync(alice);

            await alice.ExpectSinceAsync(opened, Slot(PocketType.Etc, 0, WorldData.Seed, WorldData.BoxSeeds), "the seeds in the bag");
            await alice.ExpectSinceAsync(opened, Slot(PocketType.Etc, 1, WorldData.Pebble, WorldData.BoxPebbles), "the pebbles in the bag");
            PacketAssert.AreEqual(new RdiPacket { ItemVNum = WorldData.Seed, Amount = WorldData.BoxSeeds },
                await alice.ExpectSinceAsync<RdiPacket>(opened, p => p.ItemVNum == WorldData.Seed, "the seeds announced"));
            PacketAssert.AreEqual(new RdiPacket { ItemVNum = WorldData.Pebble, Amount = WorldData.BoxPebbles },
                await alice.ExpectSinceAsync<RdiPacket>(opened, p => p.ItemVNum == WorldData.Pebble, "the pebbles announced"));
            await alice.ExpectSinceAsync(opened, EmptySlot(PocketType.Main, 0), "the box gone from the bag");
        }

        [TestMethod]
        public async Task AClassBoxGivesWhatTheOpenersClassCanUse()
        {
            await using var alice = await HoldingAsync(WorldData.ClassBox);

            var opened = alice.Mark();
            await OpenAsync(alice);

            await alice.ExpectSinceAsync(opened, Slot(PocketType.Equipment, 0, WorldData.AdventurerBlade, 0), "the adventurer's blade");
            await alice.ExpectSinceAsync(opened, EmptySlot(PocketType.Main, 0), "the box gone from the bag");
            Assert.IsTrue(await alice.NoneSinceAsync<RdiPacket>(opened, p => p.ItemVNum == WorldData.SwordsmanBlade, TimeSpan.FromSeconds(1)),
                "no swordsman's blade");
        }

        [TestMethod]
        public async Task ABoxDroppedOnTheMapOpensWhereItLiesAfterItsBar()
        {
            var player = await Harness.AddPlayerAsync("Alice");
            await Harness.GiveAsync(player, WorldData.GroundBox, 2, pocket: NoscorePocketType.Main);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);

            await alice.SendAsync(new PutPacket { PocketType = PocketType.Main, Slot = 0, Amount = 1 });
            var box = await alice.ExpectAsync<DropPacket>(p => p.VNum == WorldData.GroundBox, "the first box on the ground");
            var second = alice.Mark();
            await alice.SendAsync(new PutPacket { PocketType = PocketType.Main, Slot = 0, Amount = 1 });
            await alice.ExpectSinceAsync<DropPacket>(second, p => p.VNum == WorldData.GroundBox && p.VisualId != box.VisualId,
                "the second box on the ground");

            var picked = alice.Mark();
            await alice.SendAsync(new GetPacket { PickerType = VisualType.Player, PickerId = alice.CharacterId, VisualId = box.VisualId });
            var follow = new GetPacket
            {
                PickerType = VisualType.Player, PickerId = alice.CharacterId, VisualId = box.VisualId, IsDelayed = 1
            };
            var bar = await alice.ExpectSinceAsync<DelayPacket>(picked, null, "the opening bar");
            Assert.AreEqual((2000, DelayPacketType.OpeningBox), (bar.Delay, bar.Type));
            // The test client reads the follow-up inside the bar with the server's get shape, whose
            // fourth field is the client's delayed flag.
            var decoded = bar.Packet as ServerGetPacket;
            Assert.IsNotNull(decoded, "the follow-up inside the bar");
            PacketAssert.AreEqual(new ServerGetPacket { VisualType = VisualType.Player, VisualId = alice.CharacterId, ItemId = box.VisualId, Unknow = 1 },
                decoded);

            await Task.Delay(2100);
            var opened = alice.Mark();
            await alice.SendAsync(follow);

            await alice.ExpectSinceAsync<GroundEffectPacket>(opened, p => p.Effect == 194, "the box opening on the ground");
            await alice.ExpectSinceAsync(opened, Slot(PocketType.Etc, 0, WorldData.Pebble, WorldData.GroundBoxPebbles), "the pebbles in the bag");
            PacketAssert.AreEqual(new MsgiPacket
            {
                Type = MessageType.Default, Message = Game18NConstString.ItemReceived, ArgumentType = 1, Game18NArguments = { WorldData.Pebble }
            }, await alice.ExpectSinceAsync<MsgiPacket>(opened, p => p.Message == Game18NConstString.ItemReceived, "the received line"));
            await alice.ExpectSinceAsync<ServerGetPacket>(opened, p => p.ItemId == box.VisualId, "the box gone from the ground");
        }
    }
}
