//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.EquipmentUpgradeService;
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

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class EquipmentUpgradeOperationTests
    {
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;
        private const short SmallGemVNum = 1015;
        private const short FullGemVNum = 1016;
        private const short ScrollVNum = 1218;
        private const short DimensionalForceVNum = 2774;
        private const short ShardVNum = 2748;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private UpgradeItemOperation _unprotected = null!;
        private UpgradeItemProtectedOperation _protected = null!;
        private InventoryItemInstance _wearable = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _unprotected = new UpgradeItemOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                new EquipmentUpgradeService());
            _protected = new UpgradeItemProtectedOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer,
                new EquipmentUpgradeService());
        }

        [TestMethod]
        public async Task SuccessAtUpgradeZeroIncrementsAndChargesCellonAndGem()
        {
            await new Spec("Successful upgrade at +0 increments to +1, consumes cellon+gem, charges 500 gold")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillSucceed)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)1)
                .And(WearableShouldNotBeFixed)
                .And(CellonRemainingShouldBe_, (short)980)
                .And(SmallGemRemainingShouldBe_, (short)9)
                .And(GoldShouldBe_, 99_500L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FixedRollLocksTheItem()
        {
            await new Spec("A roll inside the Fixed band sets IsFixed=true and consumes materials")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.10)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)5)
                .And(WearableShouldBeFixed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnprotectedFailureDestroysTheWearable()
        {
            await new Spec("Unprotected failure destroys the wearable and charges materials")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedFailureRollConsumesScrollButDoesNotLockTheItem()
        {
            await new Spec("Protected failure consumes the scroll and leaves the wearable unlocked")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)5)
                .And(WearableShouldNotBeFixed)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HighRareItemUsesTheSameFailureBand()
        {
            await new Spec("Rare 8 at +7 with roll 0.50 destroys the item, as for any rarity")
                .Given(HighRareWearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HighRareItemUsesTheSameFixedBand()
        {
            await new Spec("Rare 8 at +7 with roll 0.02 locks the item, as for any rarity")
                .Given(HighRareWearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)7)
                .And(WearableShouldBeFixed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HighRareItemUsesTheSameSuccessBand()
        {
            await new Spec("Rare 8 at +7 with roll 0.96 succeeds, as for any rarity")
                .Given(HighRareWearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.96)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)8)
                .And(WearableShouldNotBeFixed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task HighRareProtectedFailureConsumesScrollButDoesNotLockTheItem()
        {
            await new Spec("Rare 8 protected failure at +7 consumes the scroll without locking the item")
                .Given(HighRareWearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)7)
                .And(WearableShouldNotBeFixed)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeAboveFiveUsesFullGemInsteadOfSmallGem()
        {
            await new Spec("Upgrade at +5+ consumes the full-gem (1016), not the small-gem (1015)")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.99)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)6)
                .And(SmallGemRemainingShouldBe_, (short)10) // unchanged
                .And(FullGemRemainingShouldBe_, (short)9)   // -1
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeFromSevenSucceedsOnlyInTheTopFivePercent()
        {
            await new Spec("At +7 a roll of 0.94 destroys the item")
                .Given(WearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.94)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeFromSevenSucceedsAtTheTop()
        {
            await new Spec("At +7 a roll of 0.96 upgrades the item to +8")
                .Given(WearableAtUpgrade_, (byte)7)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.96)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)8)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeAtMaxLevelIsRejectedSilently()
        {
            await new Spec("Upgrade at the +13 cap is rejected with no inventory or gold change")
                .Given(WearableAtUpgrade_, (byte)13)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 1_000_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FixedItemsCannotBeUpgraded()
        {
            await new Spec("A wearable with IsFixed=true is rejected upfront with a sayi + shop_end")
                .Given(WearableAtUpgrade_, (byte)2)
                .And(WearableIsLocked)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(GoldShouldBe_, 100_000L)
                .And(TwoRejectionPacketsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedUpgradeWithoutScrollIsRejected()
        {
            await new Spec("Protected upgrade with no scroll in inventory rejects with InfoiPacket")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(CellonAndGemInInventoryNoScroll)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(SingleRejectionPacketShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeFromTenUsesDimensionalForceAndShards()
        {
            await new Spec("At +10 a success spends Dimensional Force and a shard instead of a soul gem")
                .Given(WearableAtUpgrade_, (byte)10)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.999)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)11)
                .And(FullGemRemainingShouldBe_, (short)10)
                .And(DimensionalForceRemainingShouldBe_, (short)6)
                .And(ShardRemainingShouldBe_, (short)9)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMissFromTenFillsThePityGauge()
        {
            await new Spec("At +10 a protected miss adds the success chance, out of 1000, to the gauge")
                .Given(WearableAtUpgrade_, (byte)10)
                .And(PityGaugeIs_, (short)100)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)10)
                .And(PityGaugeShouldBe_, (short)105)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullPityGaugeForcesTheNextUpgradeAndEmptiesIt()
        {
            await new Spec("A full gauge turns a failing roll into a success and resets the gauge")
                .Given(WearableAtUpgrade_, (byte)11)
                .And(PityGaugeIs_, (short)1000)
                .And(MaterialsInInventory)
                .And(MoreCellaPowder)
                .And(CharacterHasGold_, 10_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)12)
                .And(PityGaugeShouldBe_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMissBelowTenLeavesThePityGaugeEmpty()
        {
            await new Spec("Below +10 a protected miss does not fill the gauge")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(PityGaugeShouldBe_, (short)0)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void PityGaugeIs_(short gauge) => ((WearableInstance)_wearable.ItemInstance).PityGauge = gauge;

        private void WearableAtUpgrade_(byte upgrade) => PlaceWearable(upgrade, 0);

        private void HighRareWearableAtUpgrade_(byte upgrade) => PlaceWearable(upgrade, 8);

        private void PlaceWearable(byte upgrade, sbyte rare)
        {
            var item = new Item { VNum = ArmorVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor };
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Upgrade = upgrade,
                Rare = rare,
            };
            _wearable = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            _wearable.Slot = 0;
            _wearable.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_wearable.ItemInstanceId] = _wearable;
        }

        private void WearableIsLocked() =>
            ((WearableInstance)_wearable.ItemInstance).IsFixed = true;

        private void MaterialsInInventory()
        {
            AddStack(CellonVNum, 1000, slot: 0);
            AddStack(SmallGemVNum, 10, slot: 1);
            AddStack(FullGemVNum, 10, slot: 2);
            AddStack(ScrollVNum, 5, slot: 3);
            AddStack(DimensionalForceVNum, 10, slot: 4);
            AddStack(ShardVNum, 10, slot: 5);
        }

        private void MoreCellaPowder() => AddStack(CellonVNum, 2000, slot: 6);

        private void CellonAndGemInInventoryNoScroll()
        {
            AddStack(CellonVNum, 1000, slot: 0);
            AddStack(SmallGemVNum, 10, slot: 1);
        }

        private void AddStack(short vnum, short amount, short slot)
        {
            var inst = InventoryItemInstance.Create(
                new ItemInstanceForTest(vnum) { Amount = amount },
                _session.Character.CharacterId);
            inst.Slot = slot;
            inst.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillSucceed() => _random.Setup(r => r.NextDouble()).Returns(0.99);

        private void NextRollWillBe_(double roll) => _random.Setup(r => r.NextDouble()).Returns(roll);

        // --- Whens ---

        private async Task UnprotectedUpgradeIsExecuted() => _result = await _unprotected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.UpgradeItem));

        private async Task ProtectedUpgradeIsExecuted() => _result = await _protected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.UpgradeItemProtected));

        private static UpgradePacket BuildPacket(UpgradePacketType type) => new()
        {
            UpgradeType = type,
            InventoryType = PocketType.Equipment,
            Slot = 0,
        };

        // --- Thens ---

        private void WearableUpgradeShouldBe_(byte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).Upgrade);

        private void WearableShouldBeFixed() =>
            Assert.AreEqual(true, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).IsFixed);

        private void WearableShouldNotBeFixed() =>
            Assert.AreNotEqual(true, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).IsFixed);

        private void SourceSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));

        private void CellonRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(CellonVNum));

        private void SmallGemRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(SmallGemVNum));

        private void FullGemRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(FullGemVNum));

        private void DimensionalForceRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(DimensionalForceVNum));

        private void ShardRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(ShardVNum));

        private void PityGaugeShouldBe_(short expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_wearable.ItemInstance).PityGauge);

        private void ScrollShouldHaveBeenConsumed() =>
            Assert.AreEqual(4, _session.Character.InventoryService.CountItem(ScrollVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        private void SingleRejectionPacketShouldBeReturned() => Assert.AreEqual(1, _result?.Count ?? 0);

        private void TwoRejectionPacketsShouldBeReturned() => Assert.AreEqual(2, _result?.Count ?? 0);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
