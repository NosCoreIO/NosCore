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
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Npcs;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    // An NPC runs only the services its shop type offers: the gambler bets on rarity and does not
    // upgrade. A worn Blessing Amulet guards the bet and is spent by it.
    [TestClass]
    public class GamblerTests
    {
        private static readonly RequestNpcPacket TalkToTheGambler = new() { Type = VisualType.Npc, TargetId = WorldData.Gambler };

        private static readonly NrunPacket OpenTheBetWindow = new()
        {
            Runner = NrunRunnerType.ProbabilityUIs, Type = (short)UpgradePacketType.RarifyItem, VisualType = VisualType.Npc,
            VisualId = WorldData.Gambler
        };

        private static readonly NrunPacket OpenTheUpgradeWindow = new()
        {
            Runner = NrunRunnerType.UpgradeItem, Type = 0, VisualType = VisualType.Npc, VisualId = WorldData.Gambler
        };

        [TestMethod]
        public async Task TheGamblerOpensTheBetWindow()
        {
            await using var client = await AtTheGamblerAsync("Alice");

            await client.SendAsync(OpenTheBetWindow);

            var window = await client.ExpectAsync<WopenPacket>();
            Assert.AreEqual(WindowType.RarifyItem, window.Type);
        }

        [TestMethod]
        public async Task TheGamblerDoesNotUpgradeItems()
        {
            await using var client = await AtTheGamblerAsync("Alice");
            var mark = client.Mark();

            await client.SendAsync(OpenTheUpgradeWindow);

            Assert.IsTrue(await client.NoneSinceAsync<WopenPacket>(mark, null, TimeSpan.FromSeconds(1)),
                "an NPC whose shop type has no upgrade service opens no upgrade window");
        }

        [TestMethod]
        public async Task ABetWithABlessingAmuletKeepsTheItemAndSpendsTheAmulet()
        {
            await using var client = await AtTheGamblerAsync("Alice", amuletUses: 1);
            await client.SendAsync(OpenTheBetWindow);
            await client.ExpectAsync<WopenPacket>();
            var mark = client.Mark();

            await client.SendAsync(new UpgradePacket { UpgradeType = UpgradePacketType.RarifyItem, InventoryType = PocketType.Equipment, Slot = 0 });

            await client.ExpectSinceAsync<SayiPacket>(mark, p => p.Message == Game18NConstString.AmuletExpired);
            Assert.IsTrue(await client.NoneSinceAsync<MsgiPacket>(mark, p => p.Message == Game18NConstString.GambleItemDisappeared, TimeSpan.Zero),
                "the amulet keeps the item whatever the roll");
        }

        [TestMethod]
        public async Task AScrollBetIsRefusedWhileAnAmuletIsWorn()
        {
            await using var client = await AtTheGamblerAsync("Alice", amuletUses: 3);
            await client.SendAsync(new NrunPacket
            {
                Runner = NrunRunnerType.ProbabilityUIs, Type = (short)UpgradePacketType.RarifyItemProtected,
                VisualType = VisualType.Npc, VisualId = WorldData.Gambler
            });
            await client.ExpectAsync<WopenPacket>();

            await client.SendAsync(new UpgradePacket
            {
                UpgradeType = UpgradePacketType.RarifyItemProtected, InventoryType = PocketType.Equipment, Slot = 0
            });

            await client.ExpectAsync<MsgiPacket>(p => p.Message == Game18NConstString.UnequipTheAmuletFirstThenYou);
            Assert.AreEqual(Gold, (await Harness.StoredAsync(client.CharacterId)).Gold, "nothing is charged");
        }

        [TestMethod]
        public async Task AChampionItemIsNotBetWithANormalAmulet()
        {
            await using var client = await AtTheGamblerAsync("Alice", amuletUses: 3, item: WorldData.ChampionBlade);
            await client.SendAsync(OpenTheBetWindow);
            await client.ExpectAsync<WopenPacket>();

            await client.SendAsync(new UpgradePacket { UpgradeType = UpgradePacketType.RarifyItem, InventoryType = PocketType.Equipment, Slot = 0 });

            await client.ExpectAsync<MsgiPacket>(p => p.Message == Game18NConstString.YouAreWearingAnIncorrectAmulet);
            Assert.AreEqual(Gold, (await Harness.StoredAsync(client.CharacterId)).Gold, "nothing is charged");
        }

        private const long Gold = 10000;

        private static async Task<TestClient> AtTheGamblerAsync(string name, int? amuletUses = null, short item = WorldData.Blade)
        {
            var player = await Harness.AddPlayerAsync(name, x: WorldData.GamblerX - 1, y: WorldData.GamblerY, configure: c => c.Gold = Gold);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = item, Amount = 1 }, NoscorePocketType.Equipment);
            await Harness.GiveAsync(player, WorldData.CellaPowder, 5);
            if (amuletUses is { } uses)
            {
                await Harness.GiveAsync(player, new WearableInstance { ItemVNum = WorldData.BlessingAmulet, Amount = 1, DurabilityPoint = uses },
                    NoscorePocketType.Wear, (short)EquipmentType.Amulet);
            }

            var client = Harness.Client(player);
            try
            {
                await client.EnterWorldAsync(player.Slot);
                await client.SendAsync(TalkToTheGambler);
                await client.ExpectAsync<NpcReqPacket>(p => p.TargetId == WorldData.Gambler);
            }
            catch
            {
                await client.DisposeAsync();
                throw;
            }

            return client;
        }
    }
}
