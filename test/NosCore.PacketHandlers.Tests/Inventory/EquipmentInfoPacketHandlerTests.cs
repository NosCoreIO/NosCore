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
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemInfoService;
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
            itemInfoService.Setup(s => s.GenerateInfo(It.Is<IItemInstance>(i => i.ItemVNum == 1))).Returns(_info);
            _handler = new EquipmentInfoPacketHandler(itemInfoService.Object);
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
