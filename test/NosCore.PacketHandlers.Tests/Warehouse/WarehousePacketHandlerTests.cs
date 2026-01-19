//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class WarehousePacketHandlerTests
    {
        private ClientSession Session = null!;
        private Mock<IWarehouseHub> WarehouseHub = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            WarehouseHub = new Mock<IWarehouseHub>();
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
        public async Task FamilyDepositPacketShouldExecuteWithoutError()
        {
            await new Spec("Family deposit packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(DepositingItemToFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyWithdrawPacketShouldExecuteWithoutError()
        {
            await new Spec("Family withdraw packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(WithdrawingItemFromFamilyWarehouse)
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

        [TestMethod]
        public async Task FamilyReposPacketShouldExecuteWithoutError()
        {
            await new Spec("Family repos packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(RearrangingFamilyWarehouseItems)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task DepositingItemToWarehouse()
        {
            var handler = new DepositPacketHandler(WarehouseHub.Object);
            await handler.ExecuteAsync(new DepositPacket(), Session);
        }

        private async Task WithdrawingItemFromWarehouse()
        {
            var handler = new WithdrawPacketHandler();
            await handler.ExecuteAsync(new WithdrawPacket(), Session);
        }

        private async Task ClosingWarehouse()
        {
            var handler = new StashEndPacketHandler();
            await handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task RearrangingWarehouseItems()
        {
            var handler = new ReposPacketHandler();
            await handler.ExecuteAsync(new ReposPacket(), Session);
        }

        private async Task DepositingItemToFamilyWarehouse()
        {
            var handler = new FDepositPacketHandler();
            await handler.ExecuteAsync(new FDepositPacket(), Session);
        }

        private async Task WithdrawingItemFromFamilyWarehouse()
        {
            var handler = new FWithdrawPacketHandler();
            await handler.ExecuteAsync(new FWithdrawPacket(), Session);
        }

        private async Task ClosingFamilyWarehouse()
        {
            var handler = new FStashEndPackettHandler();
            await handler.ExecuteAsync(new FStashEndPacket(), Session);
        }

        private async Task RearrangingFamilyWarehouseItems()
        {
            var handler = new FReposPacketHandler();
            await handler.ExecuteAsync(new FReposPacket(), Session);
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
