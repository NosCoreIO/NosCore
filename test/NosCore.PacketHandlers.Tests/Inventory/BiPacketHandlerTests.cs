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
    public class BiPacketHandlerTests : SpecBase
    {
        private BiPacketHandler BiPacketHandler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            BiPacketHandler = new BiPacketHandler(Logger, TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task DeletingItemFromMainSlotShouldClearSlot()
        {
            await new Spec("Deleting item from main slot should clear slot")
                .Given(AnItemWith999QuantityInSlot0)
                .WhenAsync(ConfirmingDeletionFromMainSlot_, 0)
                .Then(SlotShouldBeCleared)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingItemFromEquipmentSlotShouldClearInventory()
        {
            await new Spec("Deleting item from equipment slot should clear inventory")
                .Given(AnEquipmentItemInSlot_, 0)
                .WhenAsync(ConfirmingDeletionFromEquipmentSlot)
                .Then(InventoryShouldBeEmpty)
                .And(SlotShouldBeCleared)
                .ExecuteAsync();
        }

        private void AnItemWith999QuantityInSlot0()
        {
            CharacterHasItem(1012, 999);
        }

        private async Task ConfirmingDeletionFromMainSlot_(int value)
        {
            await BiPacketHandler.ExecuteAsync(new BiPacket
            {
                Option = RequestDeletionType.Confirmed,
                Slot = (byte)value,
                PocketType = PocketType.Main
            }, Session);
        }

        private void SlotShouldBeCleared()
        {
            var packet = GetLastPacket<IvnPacket>();
            Assert.IsNotNull(packet);
            Assert.IsTrue(packet.IvnSubPackets?.All(iv => iv?.Slot == 0 && iv.VNum == -1) ?? false);
        }

        private void AnEquipmentItemInSlot_(int value)
        {
            CharacterHasItem(1);
        }

        private async Task ConfirmingDeletionFromEquipmentSlot()
        {
            await BiPacketHandler.ExecuteAsync(new BiPacket
            {
                Option = RequestDeletionType.Confirmed,
                Slot = 0,
                PocketType = PocketType.Equipment
            }, Session);
        }

    }
}
