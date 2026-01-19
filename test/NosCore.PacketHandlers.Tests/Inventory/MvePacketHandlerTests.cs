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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class MvePacketHandlerTests : SpecBase
    {
        private MvePacketHandler MvePacketHandler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            MvePacketHandler = new MvePacketHandler();
        }

        [TestMethod]
        public async Task MovingEquipmentItemBetweenSlotsShouldUpdateBothSlots()
        {
            await new Spec("Moving equipment item between slots should update both slots")
                .Given(CharacterHasEquipmentItemInSlot0)
                .WhenAsync(MovingItemFromSlot0ToSlot1)
                .Then(ItemShouldBeInSlot1)
                .And(Slot0ShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingItemFromEquipmentToCostumeShouldWork()
        {
            await new Spec("Moving item from equipment to costume should work")
                .Given(CharacterHasFashionItemInEquipment)
                .WhenAsync(MovingItemFromEquipmentToCostume)
                .Then(ItemShouldBeInCostumePocket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingItemShouldSendPocketChangePackets()
        {
            await new Spec("Moving item should send pocket change packets")
                .Given(CharacterHasEquipmentItemInSlot0)
                .WhenAsync(MovingItemFromSlot0ToSlot1)
                .Then(ShouldReceivePocketChangePackets)
                .ExecuteAsync();
        }

        private void CharacterHasEquipmentItemInSlot0()
        {
            var item = ItemProvider.Create(1, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
        }

        private void CharacterHasFashionItemInEquipment()
        {
            var item = ItemProvider.Create(924, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Equipment, 0);
        }

        private async Task MovingItemFromSlot0ToSlot1()
        {
            await MvePacketHandler.ExecuteAsync(new MvePacket
            {
                Slot = 0,
                InventoryType = PocketType.Equipment,
                DestinationSlot = 1,
                DestinationInventoryType = PocketType.Equipment
            }, Session);
        }

        private async Task MovingItemFromEquipmentToCostume()
        {
            await MvePacketHandler.ExecuteAsync(new MvePacket
            {
                Slot = 0,
                InventoryType = PocketType.Equipment,
                DestinationSlot = 0,
                DestinationInventoryType = PocketType.Costume
            }, Session);
        }

        private void ItemShouldBeInSlot1()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Equipment);
            Assert.IsNotNull(item);
            Assert.AreEqual(1, item.ItemInstance.ItemVNum);
        }

        private void Slot0ShouldBeEmpty()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment);
            Assert.IsNull(item);
        }

        private void ItemShouldBeInCostumePocket()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Costume);
            Assert.IsNotNull(item);
            Assert.AreEqual(924, item.ItemInstance.ItemVNum);
        }

        private void ShouldReceivePocketChangePackets()
        {
            var packets = Session.LastPackets.OfType<IvnPacket>().ToList();
            Assert.IsTrue(packets.Count >= 2);
        }
    }
}
