//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class FDepositPacketHandlerTests
    {
        private ClientSession Session = null!;
        private FDepositPacketHandler Handler = null!;
        private IItemGenerationService ItemProvider = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new FDepositPacketHandler();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
        }

        [TestMethod]
        public async Task FamilyDepositPacketShouldExecuteWithoutError()
        {
            await new Spec("Family deposit packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(DepositingItemToFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyDepositWithItemShouldNotThrow()
        {
            await new Spec("Family deposit with item should not throw")
                .Given(CharacterIsOnMap)
                .And(CharacterHasItemInInventory)
                .WhenAsync(DepositingItemToFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyDepositFromEmptyInventoryShouldNotThrow()
        {
            await new Spec("Family deposit from empty inventory should not throw")
                .Given(CharacterIsOnMap)
                .And(CharacterHasEmptyInventory)
                .WhenAsync(DepositingItemToFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyDepositToSpecificSlotShouldNotThrow()
        {
            await new Spec("Family deposit to specific slot should not throw")
                .Given(CharacterIsOnMap)
                .And(CharacterHasItemInInventory)
                .WhenAsync(DepositingItemToSlot5)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasItemInInventory()
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 10), 0));
        }

        private void CharacterHasEmptyInventory()
        {
            Session.Character.InventoryService.Clear();
        }

        private async Task DepositingItemToFamilyWarehouse()
        {
            await Handler.ExecuteAsync(new FDepositPacket(), Session);
        }

        private async Task DepositingItemToSlot5()
        {
            await Handler.ExecuteAsync(new FDepositPacket { Slot = 5 }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
