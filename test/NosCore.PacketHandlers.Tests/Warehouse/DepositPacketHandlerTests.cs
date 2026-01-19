//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
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
    public class DepositPacketHandlerTests
    {
        private ClientSession Session = null!;
        private Mock<IWarehouseHub> WarehouseHub = null!;
        private DepositPacketHandler Handler = null!;
        private IItemGenerationService ItemProvider = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            WarehouseHub = new Mock<IWarehouseHub>();
            Handler = new DepositPacketHandler(WarehouseHub.Object);
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
        }

        [TestMethod]
        public async Task DepositPacketShouldCallWarehouseHub()
        {
            await new Spec("Deposit packet should call warehouse hub")
                .Given(CharacterIsOnMap)
                .WhenAsync(DepositingItemToWarehouse)
                .Then(WarehouseHubShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositPacketShouldSendCorrectOwnerId()
        {
            await new Spec("Deposit packet should send correct owner id")
                .Given(CharacterIsOnMap)
                .WhenAsync(DepositingItemToWarehouse)
                .Then(WarehouseHubShouldReceiveCorrectOwnerId)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositPacketShouldUseWarehouseType()
        {
            await new Spec("Deposit packet should use warehouse type")
                .Given(CharacterIsOnMap)
                .WhenAsync(DepositingItemToWarehouse)
                .Then(WarehouseHubShouldReceiveWarehouseType)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositingWithItemInInventoryShouldSucceed()
        {
            await new Spec("Depositing with item in inventory should succeed")
                .Given(CharacterIsOnMap)
                .And(CharacterHasItemInInventory)
                .WhenAsync(DepositingItemToWarehouse)
                .Then(WarehouseHubShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DepositingFromEmptyInventoryShouldStillCallHub()
        {
            await new Spec("Depositing from empty inventory should still call hub")
                .Given(CharacterIsOnMap)
                .And(CharacterHasEmptyInventory)
                .WhenAsync(DepositingItemToWarehouse)
                .Then(WarehouseHubShouldBeCalled)
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

        private async Task DepositingItemToWarehouse()
        {
            await Handler.ExecuteAsync(new DepositPacket(), Session);
        }

        private void WarehouseHubShouldBeCalled()
        {
            WarehouseHub.Verify(x => x.AddWarehouseItemAsync(It.IsAny<WareHouseDepositRequest>()), Times.Once);
        }

        private void WarehouseHubShouldReceiveCorrectOwnerId()
        {
            WarehouseHub.Verify(x => x.AddWarehouseItemAsync(
                It.Is<WareHouseDepositRequest>(r => r.OwnerId == Session.Character.CharacterId)), Times.Once);
        }

        private void WarehouseHubShouldReceiveWarehouseType()
        {
            WarehouseHub.Verify(x => x.AddWarehouseItemAsync(
                It.Is<WareHouseDepositRequest>(r => r.WarehouseType == WarehouseType.Warehouse)), Times.Once);
        }
    }
}
