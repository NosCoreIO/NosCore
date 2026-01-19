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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class FWithdrawPacketHandlerTests
    {
        private ClientSession Session = null!;
        private FWithdrawPacketHandler Handler = null!;
        private IItemGenerationService ItemProvider = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new FWithdrawPacketHandler();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
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
        public async Task FamilyWithdrawFromEmptyWarehouseShouldNotThrow()
        {
            await new Spec("Family withdraw from empty warehouse should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(WithdrawingFromSlot0)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyWithdrawFromInvalidSlotShouldNotThrow()
        {
            await new Spec("Family withdraw from invalid slot should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(WithdrawingFromInvalidSlot)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyWithdrawWithFullInventoryShouldNotThrow()
        {
            await new Spec("Family withdraw with full inventory should not throw")
                .Given(CharacterIsOnMap)
                .And(CharacterHasFullInventory)
                .WhenAsync(WithdrawingItemFromFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyWithdrawWithEmptyInventoryShouldNotThrow()
        {
            await new Spec("Family withdraw with empty inventory should not throw")
                .Given(CharacterIsOnMap)
                .And(CharacterHasEmptyInventory)
                .WhenAsync(WithdrawingItemFromFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterHasFullInventory()
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 999), 0));
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 999), 0));
        }

        private void CharacterHasEmptyInventory()
        {
            Session.Character.InventoryService.Clear();
        }

        private async Task WithdrawingItemFromFamilyWarehouse()
        {
            await Handler.ExecuteAsync(new FWithdrawPacket(), Session);
        }

        private async Task WithdrawingFromSlot0()
        {
            await Handler.ExecuteAsync(new FWithdrawPacket { Slot = 0 }, Session);
        }

        private async Task WithdrawingFromInvalidSlot()
        {
            await Handler.ExecuteAsync(new FWithdrawPacket { Slot = 999 }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
