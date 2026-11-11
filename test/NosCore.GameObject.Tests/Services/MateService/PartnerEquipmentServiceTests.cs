//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Item = NosCore.GameObject.Services.ItemGenerationService.Item.Item;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class PartnerEquipmentServiceTests
    {
        private TestWorld World;
        private const short PartnerWeapon = 990;
        private const short PartnerArmor = 995;
        private const short Gloves = 3;
        private const short PlayerWeapon = 1;
        private const short HighLevelGloves = 4;

        private ClientSession _session;
        private ItemGenerationService _items;
        private GameObject.Services.MateService.Mate _partner;
        private PartnerEquipmentService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _service = World.UseReal<IPartnerSpecialistService, PartnerSpecialistService>().Create<PartnerEquipmentService>();
            _items = new ItemGenerationService(new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = PlayerWeapon, ItemType = ItemType.Weapon },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PartnerWeapon, ItemType = ItemType.Weapon,
                    EquipmentSlot = EquipmentType.MainWeapon, ItemSubType = 12
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = PartnerArmor, ItemType = ItemType.Armor,
                    EquipmentSlot = EquipmentType.Armor, ItemSubType = 4
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = Gloves, ItemType = ItemType.Fashion,
                    EquipmentSlot = EquipmentType.Gloves
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = HighLevelGloves, ItemType = ItemType.Fashion,
                    EquipmentSlot = EquipmentType.Gloves, LevelMinimum = 50
                }
            }, NullLoggerFactory.Instance, World.LogLanguageLocalizer);
            _partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[_partner.MateTransportId] = _partner;
        }

        private void InEquipment(short vnum, short slot) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_items.Create(vnum, 1), _session.Character.CharacterId),
                NoscorePocketType.Equipment, slot);

        private InventoryItemInstance? Worn(EquipmentType slot) =>
            _session.Character.InventoryService.LoadBySlotAndType((short)slot, NoscorePocketType.FirstPartnerInventory);

        [TestMethod]
        public async Task APartnerWeaponGoesIntoTheFirstPartnersPocket()
        {
            InEquipment(PartnerWeapon, 0);

            await _service.WearAsync(_session.Character, 0, 1);

            Assert.AreEqual(PartnerWeapon, Worn(EquipmentType.MainWeapon)?.ItemInstance.ItemVNum);
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));
            var scn = _session.LastPackets.OfType<ScnPacket>().Single();
            Assert.AreEqual(PartnerWeapon, scn.WeaponInstanceDetails.NotNull().ItemId);
            Assert.AreEqual(-1, scn.ArmorInstanceDetails.NotNull().ItemId);
        }

        [TestMethod]
        public async Task WearingOverAPieceSwapsItBackIntoTheBag()
        {
            InEquipment(PartnerArmor, 0);
            InEquipment(PartnerArmor, 1);
            await _service.WearAsync(_session.Character, 0, 1);
            var first = Worn(EquipmentType.Armor).NotNull().ItemInstance.Id;

            await _service.WearAsync(_session.Character, 1, 1);

            Assert.AreNotEqual(first, Worn(EquipmentType.Armor).NotNull().ItemInstance.Id);
            Assert.AreEqual(first,
                _session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Equipment).NotNull().ItemInstance.Id);
        }

        [TestMethod]
        public async Task GlovesFitAnyPartner()
        {
            InEquipment(Gloves, 0);

            await _service.WearAsync(_session.Character, 0, 1);

            Assert.IsNotNull(Worn(EquipmentType.Gloves));
        }

        [TestMethod]
        public async Task APlayersWeaponIsRefused()
        {
            InEquipment(PlayerWeapon, 0);

            await _service.WearAsync(_session.Character, 0, 1);

            Assert.IsNull(Worn(EquipmentType.MainWeapon));
            Assert.AreEqual(Game18NConstString.PartnerCanNotUseThatEquipment,
                _session.LastPackets.OfType<SayiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task GearAboveThePartnersLevelIsRefused()
        {
            InEquipment(HighLevelGloves, 0);

            await _service.WearAsync(_session.Character, 0, 1);

            Assert.IsNull(Worn(EquipmentType.Gloves));
            Assert.AreEqual(Game18NConstString.PartnerCanNotUseThatEquipment,
                _session.LastPackets.OfType<SayiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task AnUnknownPartnerSlotDoesNothing()
        {
            InEquipment(PartnerWeapon, 0);

            await _service.WearAsync(_session.Character, 0, 2);

            Assert.IsNotNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));
            Assert.AreEqual(0, _session.LastPackets.Count);
        }

        [TestMethod]
        public async Task RemovingPutsThePieceBackInTheBag()
        {
            InEquipment(PartnerWeapon, 0);
            await _service.WearAsync(_session.Character, 0, 1);

            await _service.RemoveAsync(_session.Character, EquipmentType.MainWeapon, 1);

            Assert.IsNull(Worn(EquipmentType.MainWeapon));
            Assert.AreEqual(PartnerWeapon,
                _session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment).NotNull().ItemInstance.ItemVNum);
            Assert.AreEqual(-1, _session.LastPackets.OfType<ScnPacket>().Last().WeaponInstanceDetails.NotNull().ItemId);
        }

        [TestMethod]
        public async Task RemovingWithAFullBagKeepsThePieceOn()
        {
            InEquipment(PartnerWeapon, 0);
            await _service.WearAsync(_session.Character, 0, 1);
            InEquipment(Gloves, 0);
            InEquipment(Gloves, 1);

            await _service.RemoveAsync(_session.Character, EquipmentType.MainWeapon, 1);

            Assert.IsNotNull(Worn(EquipmentType.MainWeapon));
            Assert.AreEqual(Game18NConstString.NotEnoughSpace, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }
    }
}
