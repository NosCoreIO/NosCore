//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class RemovePacketHandlerTests : SpecBase
    {
        private RemovePacketHandler RemovePacketHandler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            RemovePacketHandler = new RemovePacketHandler();
        }

        [TestMethod]
        public async Task RemovingEquippedWeaponShouldMoveToEquipmentPocket()
        {
            await new Spec("Removing equipped weapon should move to equipment pocket")
                .Given(CharacterHasWeaponEquipped)
                .WhenAsync(RemovingMainWeapon)
                .Then(WeaponShouldBeInEquipmentPocket)
                .And(WeaponSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingNonExistentItemShouldDoNothing()
        {
            await new Spec("Removing non existent item should do nothing")
                .WhenAsync(RemovingMainWeapon)
                .Then(InventoryShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingEquippedArmorShouldWork()
        {
            await new Spec("Removing equipped armor should work")
                .Given(CharacterHasArmorEquipped)
                .WhenAsync(RemovingArmor)
                .Then(ArmorShouldBeInEquipmentPocket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingItemWithFullInventoryShouldShowNotEnoughSpaceMessage()
        {
            await new Spec("Removing item with full inventory should show not enough space message")
                .Given(CharacterHasWeaponEquipped)
                .And(EquipmentInventoryIsFull)
                .WhenAsync(RemovingMainWeapon)
                .Then(ShouldReceiveNotEnoughSpaceMessage)
                .And(WeaponShouldStillBeEquipped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingEquippedItemShouldSendPocketChangePacket()
        {
            await new Spec("Removing equipped item should send pocket change packet")
                .Given(CharacterHasWeaponEquipped)
                .WhenAsync(RemovingMainWeapon)
                .Then(ShouldReceivePocketChangePacket)
                .ExecuteAsync();
        }

        private void CharacterHasWeaponEquipped()
        {
            var item = ItemProvider.Create(1, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);
        }

        private void CharacterHasArmorEquipped()
        {
            var item = ItemProvider.Create(1, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Wear, (short)EquipmentType.Armor);
        }

        private void EquipmentInventoryIsFull()
        {
            for (short i = 0; i < TestHelpers.Instance.WorldConfiguration.Value.BackpackSize; i++)
            {
                var item = ItemProvider.Create(1, 1);
                Session.Character.InventoryService.AddItemToPocket(
                    InventoryItemInstance.Create(item, Session.Character.CharacterId),
                    NoscorePocketType.Equipment, i);
            }
        }

        private async Task RemovingMainWeapon()
        {
            await RemovePacketHandler.ExecuteAsync(new RemovePacket
            {
                InventorySlot = EquipmentType.MainWeapon
            }, Session);
        }

        private async Task RemovingArmor()
        {
            await RemovePacketHandler.ExecuteAsync(new RemovePacket
            {
                InventorySlot = EquipmentType.Armor
            }, Session);
        }

        private void WeaponShouldBeInEquipmentPocket()
        {
            var item = Session.Character.InventoryService.FirstOrDefault(i =>
                i.Value.Type == NoscorePocketType.Equipment);
            Assert.IsNotNull(item.Value);
            Assert.AreEqual(1, item.Value.ItemInstance.ItemVNum);
        }

        private void ArmorShouldBeInEquipmentPocket()
        {
            var item = Session.Character.InventoryService.FirstOrDefault(i =>
                i.Value.Type == NoscorePocketType.Equipment);
            Assert.IsNotNull(item.Value);
            Assert.AreEqual(1, item.Value.ItemInstance.ItemVNum);
        }

        private void WeaponSlotShouldBeEmpty()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(
                (short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
            Assert.IsNull(item);
        }

        private void WeaponShouldStillBeEquipped()
        {
            var item = Session.Character.InventoryService.LoadBySlotAndType(
                (short)EquipmentType.MainWeapon, NoscorePocketType.Wear);
            Assert.IsNotNull(item);
            Assert.AreEqual(1, item.ItemInstance.ItemVNum);
        }

        private void ShouldReceiveNotEnoughSpaceMessage()
        {
            var packet = GetLastPacket<MsgiPacket>();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.NotEnoughSpace, packet.Message);
        }

        private void ShouldReceivePocketChangePacket()
        {
            var packet = GetLastPacket<IvnPacket>();
            Assert.IsNotNull(packet);
        }
    }
}
