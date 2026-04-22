//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;
using DbItemInstance = NosCore.Database.Entities.ItemInstance;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class SumUpgradeOperationTests
    {
        private const short ArmorVNum = 1;
        private const short SandVNum = 1027;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private SumUpgradeOperation _operation = null!;
        private InventoryItemInstance _source = null!;
        private InventoryItemInstance _target = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _operation = new SumUpgradeOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task SuccessUpgradesSourceAndConsumesTargetAndSandAndGold()
        {
            // Source upgrade 0 → goldprice[0]=1500, sand[0]=5, success rate at sum=0 is 1.00.
            await new Spec("Successful sum upgrades source, consumes target, sand and gold")
                .Given(SourceAndTargetWearablesArePlaced)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillSucceed)
                .WhenAsync(SumIsExecuted)
                .Then(SourceUpgradeShouldBe_, (byte)1)
                .And(TargetSlotShouldBeEmpty)
                .And(SandShouldHaveBeenConsumed_, (short)5)
                .And(GoldShouldBe_, 98_500L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FailureDestroysSourceAndConsumesTargetAndChargesCosts()
        {
            // Source upgrade 1 → goldprice[1]=3000, sand[1]=10. Combined 1+1=2 → success rate 0.85.
            // Roll 0.95 lands on failure.
            await new Spec("Failed sum destroys source, consumes target, still charges costs")
                .Given(SourceAtUpgrade_AndTargetAtUpgrade_, (byte)1, (byte)1)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillFail)
                .WhenAsync(SumIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .And(TargetSlotShouldBeEmpty)
                .And(SandShouldHaveBeenConsumed_, (short)10)
                .And(GoldShouldBe_, 97_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ArmorPairIsRejectedSilentlyBecauseSumIsRestrictedToBootsAndGloves()
        {
            await new Spec("Armor + Armor sum is rejected (slot must be Boots or Gloves)")
                .Given(SourceAndTargetArmorWearablesArePlaced)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(SumIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BootsPlusGlovesIsRejectedSilentlyBecauseSlotsMustMatch()
        {
            await new Spec("Boots + Gloves sum is rejected (slot must match)")
                .Given(SourceBootsAndTargetGlovesArePlaced)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(SumIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SumOfLevelSixIsRejectedWithoutCharging()
        {
            await new Spec("Combined upgrade level 6+ is rejected with no inventory or gold change")
                .Given(SourceAtUpgrade_AndTargetAtUpgrade_, (byte)3, (byte)3)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(SumIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InsufficientGoldEmitsRejectionAndKeepsInventoryIntact()
        {
            await new Spec("Insufficient gold emits InfoiPacket and leaves items intact")
                .Given(SourceAndTargetWearablesArePlaced)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100L)
                .WhenAsync(SumIsExecuted)
                .Then(SingleRejectionPacketShouldBeReturned)
                .And(SourceSlotShouldStillHoldOriginalSourceItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InsufficientSandEmitsRejection()
        {
            await new Spec("Insufficient sand emits InfoiPacket and leaves items intact")
                .Given(SourceAndTargetWearablesArePlaced)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(SumIsExecuted)
                .Then(SingleRejectionPacketShouldBeReturned)
                .And(SourceSlotShouldStillHoldOriginalSourceItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessAddsBothTargetInstanceAndTargetBaseItemResistance()
        {
            await new Spec("Sum success adds target.FireResistance + target.Item.FireResistance onto source")
                .Given(TargetWithInstanceResistance_AndBaseItemResistance_, (short)3, (short)5)
                .And(EnoughSandIsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillSucceed)
                .WhenAsync(SumIsExecuted)
                .Then(SourceUpgradeShouldBe_, (byte)1)
                .And(SourceFireResistanceShouldBe_, (short)8)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void SourceAndTargetWearablesArePlaced()
        {
            SourceAtUpgrade_AndTargetAtUpgrade_(0, 0);
        }

        private void SourceAtUpgrade_AndTargetAtUpgrade_(byte sourceUpgrade, byte targetUpgrade)
        {
            var inv = _session.Character.InventoryService;
            _source = MakeWearableInstance(slot: 0, ArmorVNum, upgrade: sourceUpgrade, EquipmentType.Boots);
            _target = MakeWearableInstance(slot: 1, ArmorVNum, upgrade: targetUpgrade, EquipmentType.Boots);
            inv[_source.ItemInstanceId] = _source;
            inv[_target.ItemInstanceId] = _target;
        }

        private void SourceAndTargetArmorWearablesArePlaced()
        {
            var inv = _session.Character.InventoryService;
            _source = MakeWearableInstance(slot: 0, ArmorVNum, upgrade: 0, EquipmentType.Armor);
            _target = MakeWearableInstance(slot: 1, ArmorVNum, upgrade: 0, EquipmentType.Armor);
            inv[_source.ItemInstanceId] = _source;
            inv[_target.ItemInstanceId] = _target;
        }

        private void SourceBootsAndTargetGlovesArePlaced()
        {
            var inv = _session.Character.InventoryService;
            _source = MakeWearableInstance(slot: 0, ArmorVNum, upgrade: 0, EquipmentType.Boots);
            _target = MakeWearableInstance(slot: 1, ArmorVNum, upgrade: 0, EquipmentType.Gloves);
            inv[_source.ItemInstanceId] = _source;
            inv[_target.ItemInstanceId] = _target;
        }

        private void TargetWithInstanceResistance_AndBaseItemResistance_(short instance, short baseItem)
        {
            var inv = _session.Character.InventoryService;
            _source = MakeWearableInstance(slot: 0, ArmorVNum, upgrade: 0, EquipmentType.Boots);
            _target = MakeWearableInstance(slot: 1, ArmorVNum, upgrade: 0, EquipmentType.Boots);
            ((WearableInstance)_target.ItemInstance).FireResistance = instance;
            ((WearableInstance)_target.ItemInstance).Item.FireResistance = baseItem;
            inv[_source.ItemInstanceId] = _source;
            inv[_target.ItemInstanceId] = _target;
        }

        private void EnoughSandIsInInventory()
        {
            var sand = InventoryItemInstance.Create(
                new ItemInstanceForTest(SandVNum) { Amount = 99 },
                _session.Character.CharacterId);
            sand.Slot = 0;
            sand.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[sand.ItemInstanceId] = sand;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillSucceed() => _random.Setup(r => r.NextDouble()).Returns(0.0);

        private void NextRollWillFail() => _random.Setup(r => r.NextDouble()).Returns(0.99);

        // --- Whens ---

        private async Task SumIsExecuted()
        {
            _result = await _operation.ExecuteAsync(_session, new UpgradePacket
            {
                UpgradeType = UpgradePacketType.SumResistance,
                InventoryType = PocketType.Equipment,
                Slot = 0,
                InventoryType2 = PocketType.Equipment,
                Slot2 = 1,
            });
        }

        // --- Thens ---

        private void SourceUpgradeShouldBe_(byte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).Upgrade);

        private void SourceFireResistanceShouldBe_(short expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).FireResistance);

        private void TargetSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(1, NoscorePocketType.Equipment));

        private void SourceSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));

        private void SourceSlotShouldStillHoldOriginalSourceItem() =>
            Assert.AreEqual(_source.ItemInstanceId,
                _session.Character.InventoryService
                    .LoadBySlotAndType(0, NoscorePocketType.Equipment)?.ItemInstanceId);

        private void SandShouldHaveBeenConsumed() => SandShouldHaveBeenConsumed_(1);

        private void SandShouldHaveBeenConsumed_(short amount) =>
            Assert.AreEqual(99 - amount, _session.Character.InventoryService.CountItem(SandVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        private void SingleRejectionPacketShouldBeReturned() => Assert.AreEqual(1, _result?.Count ?? 0);

        // --- Helpers ---

        private InventoryItemInstance MakeWearableInstance(short slot, short vnum, byte upgrade,
            EquipmentType equipmentSlot)
        {
            var item = new Item
            {
                VNum = vnum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Armor,
                EquipmentSlot = equipmentSlot,
            };
            var wearable = new WearableInstance(item, new Mock<Serilog.ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Upgrade = upgrade,
            };
            var inv = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            inv.Slot = slot;
            inv.Type = NoscorePocketType.Equipment;
            return inv;
        }

        // Tiny stand-in so we don't need the full ItemGenerationService just to put a stack of sand in inventory.
        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
