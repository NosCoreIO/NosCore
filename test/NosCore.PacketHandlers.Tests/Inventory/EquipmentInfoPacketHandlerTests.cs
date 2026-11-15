//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class EquipmentInfoPacketHandlerTests : SpecBase
    {
        private readonly EInfoWeaponPacket _info = new() { ItemVNum = 1 };
        private EquipmentInfoPacketHandler _handler;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            var itemInfoService = new Mock<IItemInfoService>();
            itemInfoService.Setup(s => s.GenerateInfo(It.Is<IItemInstance>(i => i.ItemVNum == 1 || i.ItemVNum == 912))).Returns(_info);
            var itemCatalog = new Mock<IItemCatalog>();
            itemCatalog.Setup(c => c.GetItem(1)).Returns(new ItemDto { VNum = 1 });
            _handler = new EquipmentInfoPacketHandler(itemInfoService.Object, itemCatalog.Object, ItemProvider);
        }

        [TestMethod]
        public async Task RightClickingAWeaponInTheBagShowsItsDetails()
        {
            await new Spec("Right clicking a weapon in the bag shows its details")
                .Given(AWeaponIsInEquipmentSlot_, (short)0)
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.Equipment, (short)0)
                .Then(TheDetailsAreSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RightClickingAWornWeaponShowsItsDetails()
        {
            await new Spec("Right clicking a worn weapon shows its details")
                .Given(AWeaponIsWorn)
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.Worn, (short)EquipmentType.MainWeapon)
                .Then(TheDetailsAreSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RightClickingAnItemInTheCardInventoryShowsItsDetails()
        {
            await new Spec("Right clicking an item in the card inventory shows its details")
                .Given(Item_IsIn_Slot_, (short)912, NoscorePocketType.Specialist, (short)2)
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.Specialist, (short)2)
                .Then(TheDetailsAreSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RightClickingAnItemInTheCostumeInventoryShowsItsDetails()
        {
            await new Spec("Right clicking an item in the costume inventory shows its details")
                .Given(Item_IsIn_Slot_, (short)1, NoscorePocketType.Costume, (short)1)
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.Costume, (short)1)
                .Then(TheDetailsAreSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnNpcShopItemIsLookedUpByItsVNum()
        {
            await new Spec("An NPC shop item is looked up by its VNum")
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.NpcShop, (short)1)
                .Then(TheDetailsAreSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownNpcShopItemSendsNothing()
        {
            await new Spec("An unknown NPC shop item sends nothing")
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.NpcShop, (short)30000)
                .Then(NothingIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptySlotSendsNothing()
        {
            await new Spec("An empty slot sends nothing")
                .WhenAsync(AskingForInfoOn_Slot_, EquipmentInfoType.Equipment, (short)0)
                .Then(NothingIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnhandledSourceSendsNothing()
        {
            await new Spec("An unhandled source sends nothing")
                .Given(AWeaponIsInEquipmentSlot_, (short)0)
                .WhenAsync(AskingForInfoOn_Slot_, (EquipmentInfoType)5, (short)0)
                .Then(NothingIsSent)
                .ExecuteAsync();
        }

        private void AWeaponIsInEquipmentSlot_(short slot) =>
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1, 1), Session.Character.CharacterId),
                NoscorePocketType.Equipment, slot);

        private void Item_IsIn_Slot_(short vnum, NoscorePocketType pocket, short slot) =>
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(vnum, 1), Session.Character.CharacterId), pocket, slot);

        private void AWeaponIsWorn() =>
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1, 1), Session.Character.CharacterId),
                NoscorePocketType.Wear, (short)EquipmentType.MainWeapon);

        private Task AskingForInfoOn_Slot_(EquipmentInfoType type, short slot) =>
            _handler.ExecuteAsync(new EquipmentInfoPacket { Type = type, Slot = slot }, Session);

        private void TheDetailsAreSent() => Assert.AreSame(_info, GetLastPacket<EInfoWeaponPacket>());

        private void NothingIsSent() => Assert.IsFalse(Session.LastPackets.Any());
    }
}
