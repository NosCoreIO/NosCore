//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class VitalityServiceTests
    {
        private const short PlainArmourVnum = 910;
        private const short HeavyArmourVnum = 911;
        private const short BlessedHatVnum = 912;

        private static readonly List<ItemDto> Catalog = new()
        {
            new Item
            {
                VNum = PlainArmourVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor,
                EquipmentSlot = EquipmentType.Armor, CloseDefence = 20
            },
            new Item
            {
                VNum = HeavyArmourVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor,
                EquipmentSlot = EquipmentType.Armor, CloseDefence = 20, Hp = 1000, Mp = 300
            },
            new Item
            {
                VNum = BlessedHatVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Hat
            },
        };

        // BCard.dat type 33 subtype 11: "Maximum HP is increased by %s."
        private static readonly List<BCardDto> ItemEffects = new()
        {
            new BCardDto
            {
                ItemVNum = BlessedHatVnum,
                Type = BCardEffect.MaxHpmpMaximumHpIncreased.Type(),
                SubType = BCardEffect.MaxHpmpMaximumHpIncreased.SubType(),
                FirstData = 500
            }
        };

        private VitalityService _service = null!;
        private ClientSession _session = null!;
        private IItemGenerationService _items = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _items = new ItemGenerationService(Catalog, NullLoggerFactory.Instance,
                TestHelpers.Instance.LogLanguageLocalizer);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());

            _service = new VitalityService(new HpService(), new MpService(),
                new EquipmentStatsService(new CardCatalog(new List<CardDto>(), ItemEffects)),
                buffs.Object);
        }

        private void Wear(EquipmentType slot, short vnum)
        {
            var item = _items.Create(vnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _session.Character.CharacterId),
                NoscorePocketType.Wear, (short)slot);
        }

        [TestMethod]
        public void NothingWornMeansTheClassAndLevelMaximum()
        {
            _service.Refresh(_session.Character);

            Assert.AreEqual((int)new HpService().GetHp(_session.Character.Class, _session.Character.Level),
                _session.Character.MaxHp);
        }

        [TestMethod]
        public void APieceWithHpRaisesTheMaximum()
        {
            _service.Refresh(_session.Character);
            var bare = _session.Character.MaxHp;
            var bareMp = _session.Character.MaxMp;

            Wear(EquipmentType.Armor, HeavyArmourVnum);
            _service.Refresh(_session.Character);

            Assert.AreEqual(bare + 1000, _session.Character.MaxHp);
            Assert.AreEqual(bareMp + 300, _session.Character.MaxMp);
        }

        [TestMethod]
        public void ATypeThirtyThreeEffectRaisesTheMaximum()
        {
            _service.Refresh(_session.Character);
            var bare = _session.Character.MaxHp;

            // The hat has no Hp field at all: everything it gives comes from the effect.
            Wear(EquipmentType.Hat, BlessedHatVnum);
            _service.Refresh(_session.Character);

            Assert.AreEqual(bare + 500, _session.Character.MaxHp);
        }

        [TestMethod]
        public void APieceWithoutHpChangesNothing()
        {
            _service.Refresh(_session.Character);
            var bare = _session.Character.MaxHp;

            Wear(EquipmentType.Armor, PlainArmourVnum);
            _service.Refresh(_session.Character);

            Assert.AreEqual(bare, _session.Character.MaxHp);
        }

        [TestMethod]
        public void LosingAPieceBringsCurrentHpBackUnderTheMaximum()
        {
            Wear(EquipmentType.Armor, HeavyArmourVnum);
            _service.Refresh(_session.Character);
            _session.Character.Hp = _session.Character.MaxHp;
            var full = _session.Character.Hp;

            _session.Character.InventoryService.Clear();
            _service.Refresh(_session.Character);

            Assert.IsTrue(_session.Character.Hp <= _session.Character.MaxHp);
            Assert.IsTrue(_session.Character.Hp < full);
        }

        [TestMethod]
        public void RefreshSaysWhetherAnythingChanged()
        {
            _service.Refresh(_session.Character);
            Assert.IsFalse(_service.Refresh(_session.Character));

            Wear(EquipmentType.Armor, HeavyArmourVnum);
            Assert.IsTrue(_service.Refresh(_session.Character));
            Assert.IsFalse(_service.Refresh(_session.Character));
        }

        [TestMethod]
        public void ALevelGainRaisesTheMaximum()
        {
            _service.Refresh(_session.Character);
            var atStart = _session.Character.MaxHp;

            _session.Character.Level += 10;
            _service.Refresh(_session.Character);

            Assert.IsTrue(_session.Character.MaxHp > atStart);
        }
    }
}
