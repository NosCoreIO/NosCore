//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class RarityGambleTests
    {
        private const long Gold = 100_000;

        private static readonly RequestNpcPacket TalkToTheGambler = new() { Type = VisualType.Npc, TargetId = WorldData.Gambler };

        private static readonly NrunPacket OpenTheBetWindow = new()
        {
            Runner = NrunRunnerType.ProbabilityUIs, Type = (short)UpgradePacketType.RarifyItem,
            VisualType = VisualType.Npc, VisualId = WorldData.Gambler
        };

        private static readonly UpgradePacket GambleTheBlade = new()
        {
            UpgradeType = UpgradePacketType.RarifyItem, InventoryType = PocketType.Equipment, Slot = 0
        };

        // A character by the gambler wearing the event amulet, with a blade, Cella Powder and gold.
        private static async Task<Player> GamblerAsync(bool bladeIsBound)
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Town, WorldData.GamblerX, WorldData.GamblerY + 1,
                c => c.Gold = Gold);
            await Harness.GiveAsync(player, new WearableInstance
            {
                ItemVNum = WorldData.Blade, Amount = 1, BoundCharacterId = bladeIsBound ? player.CharacterId : null
            }, NoscorePocketType.Equipment);
            await Harness.GiveAsync(player, new WearableInstance
            {
                ItemVNum = WorldData.EventAmulet, Amount = 1, DurabilityPoint = WorldData.EventAmuletUses
            }, NoscorePocketType.Wear, (short)EquipmentType.Amulet);
            await Harness.GiveAsync(player, WorldData.CellaPowder, 100, pocket: NoscorePocketType.Etc);
            return player;
        }

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

        private static async Task OpenTheBetWindowAsync(TestClient client)
        {
            await client.SendAsync(TalkToTheGambler);
            await client.ExpectAsync<NpcReqPacket>(p => p.Type == VisualType.Npc && p.TargetId == WorldData.Gambler,
                "the gambler's dialog");
            await client.SendAsync(OpenTheBetWindow);
            await client.ExpectAsync<WopenPacket>(p => p.Type == (WindowType)UpgradePacketType.RarifyItem, "the bet window");
        }

        [TestMethod]
        public async Task AnEventAmuletRefusesABladeThatIsNotBound()
        {
            var player = await GamblerAsync(bladeIsBound: false);
            await using var alice = await EnterAsync(player);
            await OpenTheBetWindowAsync(alice);

            var gambled = alice.Mark();
            await alice.SendAsync(GambleTheBlade);

            PacketAssert.AreEqual(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.CanOnlyBeUsedOnBoundItems },
                await alice.ExpectSinceAsync<MsgiPacket>(gambled, null, "the refusal"));
            PacketAssert.AreEqual(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
                await alice.ExpectSinceAsync<ShopEndPacket>(gambled, null, "the bet window closing"));
            Assert.IsTrue(await alice.NoneSinceAsync<GoldPacket>(gambled, null, TimeSpan.FromSeconds(1)), "gold was taken");
        }

        [TestMethod]
        public async Task AnEventAmuletGuardsABladeBoundToTheGambler()
        {
            var player = await GamblerAsync(bladeIsBound: true);
            await using var alice = await EnterAsync(player);
            await OpenTheBetWindowAsync(alice);

            var gambled = alice.Mark();
            await alice.SendAsync(GambleTheBlade);

            var outcome = await alice.ExpectSinceAsync<MsgiPacket>(gambled,
                p => p.Message is Game18NConstString.GambleSuccessful or Game18NConstString.ItemSurvivedWithProtection
                    or Game18NConstString.CanOnlyBeUsedOnBoundItems, "the gamble's outcome");
            Assert.AreNotEqual(Game18NConstString.CanOnlyBeUsedOnBoundItems, outcome.Message);
            await alice.ExpectSinceAsync<GoldPacket>(gambled, p => p.Gold == Gold - 500, "the gamble's price");
        }
    }
}
