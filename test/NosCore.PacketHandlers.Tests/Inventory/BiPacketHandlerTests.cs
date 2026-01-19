//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

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
