//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class WarehousePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IWarehouseHub> WarehouseHub;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            WarehouseHub = World.Freeze<Mock<IWarehouseHub>>();
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
        public async Task WithdrawPacketShouldExecuteWithoutError()
        {
            await new Spec("Withdraw packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(WithdrawingItemFromWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StashEndPacketShouldExecuteWithoutError()
        {
            await new Spec("Stash end packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReposPacketShouldExecuteWithoutError()
        {
            await new Spec("Repos packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(RearrangingWarehouseItems)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyStashEndPacketShouldExecuteWithoutError()
        {
            await new Spec("Family stash end packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private async Task DepositingItemToWarehouse()
        {
            var handler = World.Create<DepositPacketHandler>();
            await handler.ExecuteAsync(new DepositPacket(), Session);
        }

        private async Task WithdrawingItemFromWarehouse()
        {
            var handler = World.Create<WithdrawPacketHandler>();
            await handler.ExecuteAsync(new WithdrawPacket(), Session);
        }

        private async Task ClosingWarehouse()
        {
            var handler = World.Create<StashEndPacketHandler>();
            await handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task RearrangingWarehouseItems()
        {
            var handler = World.Create<ReposPacketHandler>();
            await handler.ExecuteAsync(new ReposPacket(), Session);
        }

        private async Task ClosingFamilyWarehouse()
        {
            var handler = World.Create<FStashEndPackettHandler>();
            await handler.ExecuteAsync(new FStashEndPacket(), Session);
        }

        private void WarehouseHubShouldBeCalled()
        {
            WarehouseHub.Verify(x => x.AddWarehouseItemAsync(It.IsAny<NosCore.Data.WebApi.WareHouseDepositRequest>()), Times.Once);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
