//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    // Cellon rolls are driven by a single stubbed NextDouble, so a roll of 0.10 at a level-1
    // cellon picks the first option (Hp, 30..100) and lands 10% into its range.
    [TestClass]
    public class CellonOperationTests
    {
        private const short JewelVNum = 900;
        private const short CellonVNum = 1017;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private Mock<IDao<EquipmentOptionDto, Guid>> _optionDao = null!;
        private List<EquipmentOptionDto> _existingOptions = null!;
        private List<EquipmentOptionDto> _persisted = null!;
        private CellonOperation _operation = null!;
        private InventoryItemInstance _jewel = null!;
        private InventoryItemInstance _cellon = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _existingOptions = new List<EquipmentOptionDto>();
            _persisted = new List<EquipmentOptionDto>();

            _optionDao = new Mock<IDao<EquipmentOptionDto, Guid>>();
            _optionDao.Setup(d => d.Where(It.IsAny<Expression<Func<EquipmentOptionDto, bool>>>()))
                .Returns(() => _existingOptions);
            _optionDao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<EquipmentOptionDto>()))
                .Returns((EquipmentOptionDto dto) =>
                {
                    _persisted.Add(dto);
                    return Task.FromResult(dto);
                });

            _operation = new CellonOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                _optionDao.Object);
        }

        [TestMethod]
        public async Task SuccessAddsAnOptionAndChargesTheCellonAndGold()
        {
            await new Spec("A successful level-1 cellon adds one option, bumps the counter and charges 700 gold")
                .Given(JewelWith_, (byte)0)
                .And(CellonOfLevel_, 1)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.10)
                .WhenAsync(CellonIsApplied)
                .Then(PersistedOptionCountShouldBe_, 1)
                .And(PersistedOptionTypeShouldBe_, CellonType.Hp)
                .And(PersistedOptionValueShouldBe_, 37)
                .And(JewelCellonCountShouldBe_, (byte)1)
                .And(GoldShouldBe_, 99_300L)
                .And(CellonSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FailureConsumesTheCellonWithoutAddingAnOption()
        {
            await new Spec("A failed roll still burns the cellon and the gold but leaves the jewel untouched")
                .Given(JewelWith_, (byte)0)
                .And(CellonOfLevel_, 1)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.90)
                .WhenAsync(CellonIsApplied)
                .Then(PersistedOptionCountShouldBe_, 0)
                .And(JewelCellonCountShouldBe_, (byte)0)
                .And(GoldShouldBe_, 99_300L)
                .And(CellonSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CellonAboveTheJewelTierIsRejected()
        {
            await new Spec("A cellon stronger than the jewel accepts is refused before anything is charged")
                .Given(JewelWith_, (byte)0)
                .And(CellonOfLevel_, 5)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.10)
                .WhenAsync(CellonIsApplied)
                .Then(NoPacketsShouldBeReturned)
                .And(GoldShouldBe_, 100_000L)
                .And(PersistedOptionCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FullJewelIsRejected()
        {
            await new Spec("A jewel already holding its maximum options is refused")
                .Given(JewelWith_, (byte)2)
                .And(CellonOfLevel_, 1)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.10)
                .WhenAsync(CellonIsApplied)
                .Then(NoPacketsShouldBeReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExhaustedOptionTypesFailInsteadOfReportingSuccess()
        {
            await new Spec("When every option the tier offers is already on the jewel the attempt fails")
                .Given(JewelWith_, (byte)1)
                .And(CellonOfLevel_, 1)
                .And(AllLevelOneOptionsAlreadyTaken)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.10)
                .WhenAsync(CellonIsApplied)
                .Then(PersistedOptionCountShouldBe_, 0)
                .And(JewelCellonCountShouldBe_, (byte)1)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void JewelWith_(byte appliedOptions)
        {
            var item = new Item
            {
                VNum = JewelVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Jewelery,
                MaxCellon = 2,
                MaxCellonLvl = 1,
            };
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Cellon = appliedOptions,
            };
            _jewel = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            _jewel.Slot = 0;
            _jewel.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_jewel.ItemInstanceId] = _jewel;
        }

        private void CellonOfLevel_(int level)
        {
            var instance = new CellonItemForTest(CellonVNum, level) { Amount = 1 };
            _cellon = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            _cellon.Slot = 3;
            _cellon.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[_cellon.ItemInstanceId] = _cellon;
        }

        private void AllLevelOneOptionsAlreadyTaken() =>
            _existingOptions.AddRange(new[] { CellonType.Hp, CellonType.Mp, CellonType.HpRecovery, CellonType.MpRecovery }
                .Select(t => new EquipmentOptionDto { Type = (byte)t, WearableInstanceId = _jewel.ItemInstanceId }));

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillBe_(double roll) => _random.Setup(r => r.NextDouble()).Returns(roll);

        // --- Whens ---

        private async Task CellonIsApplied() => _result = await _operation.ExecuteAsync(_session, new UpgradePacket
        {
            UpgradeType = UpgradePacketType.CellonItem,
            InventoryType = PocketType.Equipment,
            Slot = 0,
            CellonInventoryType = PocketType.Main,
            CellonSlot = 3,
        });

        // --- Thens ---

        private void PersistedOptionCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _persisted.Count);

        private void PersistedOptionTypeShouldBe_(CellonType expected) =>
            Assert.AreEqual((byte)expected, _persisted[0].Type);

        private void PersistedOptionValueShouldBe_(int expected) =>
            Assert.AreEqual(expected, _persisted[0].Value);

        private void JewelCellonCountShouldBe_(byte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_jewel.ItemInstance!).Cellon ?? 0);

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void CellonSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService
                .LoadBySlotAndType(3, NoscorePocketType.Main));

        private void NoPacketsShouldBeReturned() => Assert.AreEqual(0, _result!.Count);

        private sealed class CellonItemForTest(short vnum, int effectValue) : ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();

            public new short ItemVNum { get; set; } = vnum;

            public Item Item { get; set; } = new()
            {
                VNum = vnum,
                Type = NoscorePocketType.Main,
                EffectValue = effectValue,
            };

            public object Clone() => MemberwiseClone();
        }
    }
}
