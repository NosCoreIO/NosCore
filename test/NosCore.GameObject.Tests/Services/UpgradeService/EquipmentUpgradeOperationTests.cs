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

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    // Locks in the OpenNos-faithful equipment-upgrade behavior:
    //   - 3-way roll: rnd < upfix → Fixed, rnd < upfix+upfail → Failure, else → Success
    //   - For Rare<8 at Upgrade 0: upfix=0, upfail=0 → 100% success path (rnd < 0 is false twice)
    //   - For Rare<8 at Upgrade 5: upfix=20, upfail=40 → 20% Fixed, 40% Fail, 40% Success
    //   - Materials: Cellon (1014) + Gem (1015 if Upgrade<5 else 1016) + Scroll (1218) for Protected
    [TestClass]
    public class EquipmentUpgradeOperationTests
    {
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;
        private const short SmallGemVNum = 1015;
        private const short FullGemVNum = 1016;
        private const short ScrollVNum = 1218;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private UpgradeItemOperation _unprotected = null!;
        private UpgradeItemProtectedOperation _protected = null!;
        private InventoryItemInstance _wearable = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _unprotected = new UpgradeItemOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer);
            _protected = new UpgradeItemProtectedOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task SuccessAtUpgradeZeroIncrementsAndChargesCellonAndGem()
        {
            // Upgrade 0, low-rare: upfix=0, upfail=0, gold=500, cellon=20, gem=1.
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
            // Upgrade 5, low-rare: upfix=20 → roll 0.10 falls in the Fixed band.
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
            // Upgrade 5, low-rare: upfix=20, upfail=40 → roll 0.50 falls in Failure band (0.20..0.60).
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
        public async Task ProtectedFailureRollIsConvertedToFixedNoOpAndConsumesScroll()
        {
            // Upgrade 5: roll 0.50 would be a Failure for unprotected; protected absorbs it
            // (treated as Fixed-like no-op) and consumes the scroll.
            await new Spec("Protected failure roll consumes scroll, leaves wearable but locks it")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)5)
                .And(WearableShouldBeFixed)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeAboveFiveUsesFullGemInsteadOfSmallGem()
        {
            // Upgrade 5+ uses FullGem (vnum 1016) per OpenNos.
            await new Spec("Upgrade at +5+ consumes the full-gem (1016), not the small-gem (1015)")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.99)  // success band for upgrade 5 starts at 0.60
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)6)
                .And(SmallGemRemainingShouldBe_, (short)10) // unchanged
                .And(FullGemRemainingShouldBe_, (short)9)   // -1
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeAtMaxLevelIsRejectedSilently()
        {
            await new Spec("Upgrade at +10 cap is rejected with no inventory or gold change")
                .Given(WearableAtUpgrade_, (byte)10)
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
            await new Spec("A wearable with IsFixed=true is rejected upfront")
                .Given(WearableAtUpgrade_, (byte)2)
                .And(WearableIsLocked)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
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

        // --- Givens ---

        private void WearableAtUpgrade_(byte upgrade)
        {
            var item = new Item { VNum = ArmorVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor };
            var wearable = new WearableInstance(item, new Mock<Serilog.ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Upgrade = upgrade,
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
        }

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

        private void ScrollShouldHaveBeenConsumed() =>
            Assert.AreEqual(4, _session.Character.InventoryService.CountItem(ScrollVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        private void SingleRejectionPacketShouldBeReturned() => Assert.AreEqual(1, _result?.Count ?? 0);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
