//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class ItemInfoTests
    {
        private static async Task<TestClient> CarryingAsync(short vnum, NoscorePocketType pocket, short x = 80, short y = 116,
            short mapId = WorldData.Town)
        {
            var player = await Harness.AddPlayerAsync("Alice", mapId, x, y);
            await Harness.GiveAsync(player, new WearableInstance { ItemVNum = vnum, Amount = 1 }, pocket);
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

        [TestMethod]
        public async Task ACostumeHatInTheBagShowsItIsPermanentAndBound()
        {
            await using var alice = await CarryingAsync(WorldData.CostumeHat, NoscorePocketType.Equipment);

            var asked = alice.Mark();
            await alice.SendAsync(new EquipmentInfoPacket { Type = EquipmentInfoType.Equipment, Slot = 0 });

            PacketAssert.AreEqual(new EInfoCostumeHatPacket
            {
                ItemVNum = WorldData.CostumeHat,
                IsTradable = false,
                RemainingHours = -1,
            }, await alice.ExpectSinceAsync<EInfoCostumeHatPacket>(asked, null, "the costume hat's window"));
        }

        [TestMethod]
        public async Task ACostumeInTheCostumeBagIsFoundThere()
        {
            await using var alice = await CarryingAsync(WorldData.CostumeHat, NoscorePocketType.Costume);

            var asked = alice.Mark();
            await alice.SendAsync(new EquipmentInfoPacket { Type = EquipmentInfoType.Costume, Slot = 0 });

            PacketAssert.AreEqual(new EInfoCostumeHatPacket
            {
                ItemVNum = WorldData.CostumeHat,
                IsTradable = false,
                RemainingHours = -1,
            }, await alice.ExpectSinceAsync<EInfoCostumeHatPacket>(asked, null, "the costume hat's window"));
        }

        [TestMethod]
        public async Task AnAmuletNeverWornShowsItsWholeDuration()
        {
            await using var alice = await CarryingAsync(WorldData.Amulet, NoscorePocketType.Equipment);

            var asked = alice.Mark();
            await alice.SendAsync(new EquipmentInfoPacket { Type = EquipmentInfoType.Equipment, Slot = 0 });

            PacketAssert.AreEqual(new EInfoAmuletPacket
            {
                ItemVNum = WorldData.Amulet,
                LevelMinimum = WorldData.AmuletLevel,
                RemainingTime = WorldData.AmuletSeconds * 10,
                Price = 200,
            }, await alice.ExpectSinceAsync<EInfoAmuletPacket>(asked, null, "the amulet's window"));
        }

        [TestMethod]
        public async Task AnItemOnAShopShelfIsDescribedByItsNumber()
        {
            await using var alice = await Harness.EnterAsync("Alice", WorldData.Market, WorldData.GrocerX, WorldData.GrocerY + 1);

            var asked = alice.Mark();
            await alice.SendAsync(new EquipmentInfoPacket { Type = EquipmentInfoType.NpcShop, Slot = WorldData.Blade });

            PacketAssert.AreEqual(new EInfoWeaponPacket
            {
                ItemVNum = WorldData.Blade,
                Price = WorldData.BladePrice,
            }, await alice.ExpectSinceAsync<EInfoWeaponPacket>(asked, null, "the blade's window"));
        }
    }
}
