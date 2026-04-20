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
    // Locks in the OpenNos-faithful rarify behavior:
    //   - NOT a "+1" — it's a probability-band reroll where the new Rare is chosen by
    //     walking the bands from rare8 down. The first band the roll falls into wins.
    //   - Bands (out of 100): rare8=1, rare7=2, rare6=3, rare5=5, rare4=10, rare3=15,
    //     rare2=30, rare1=40, rare0=60.
    //   - Materials: Cellon (1014) × 5 + Gold 500. Protected adds Scroll (1218) × 1
    //     which clamps the new Rare so it never drops below the original.
    [TestClass]
    public class RarifyOperationTests
    {
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;
        private const short ScrollVNum = 1218;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private RarifyOperation _unprotected = null!;
        private RarifyProtectedOperation _protected = null!;
        private InventoryItemInstance _wearable = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _unprotected = new RarifyOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer);
            _protected = new RarifyProtectedOperation(_random.Object, TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task LowRollLandsOnHighestRareAndIsCountedAsSuccess()
        {
            // Roll 0.005 → rnd=0.5 → falls in rare8 band (< 1). New Rare = 8.
            await new Spec("Roll inside the rare8 band rarifies up to 8")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.005)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)8)
                .And(CellonRemainingShouldBe_, (short)95)
                .And(GoldShouldBe_, 99_500L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RollLandingOnLowerRareIsCountedAsFailure()
        {
            // Roll 0.50 → rnd=50 → first band rnd<60 is rare0. New Rare = 0.
            // Original was 5 → demoted: counts as Failure but still writes the new value.
            await new Spec("Roll that lands on a lower rare than original demotes the wearable")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollClampsNewRareAtOriginal()
        {
            // Roll 0.50 → would land on rare0; with protection the new rare is clamped
            // to original (5). Scroll is consumed.
            await new Spec("Protected scroll prevents rare from dropping below original")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)5)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollStillAllowsImprovementToHigherRare()
        {
            // Roll 0.005 → rare8 band; protection doesn't clamp upward, only downward.
            await new Spec("Protected scroll still allows the rare to improve")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.005)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)8)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RarityAtMaxIsRejectedSilently()
        {
            await new Spec("Rarify at +8 cap is rejected with no charges")
                .Given(WearableAtRarity_, (sbyte)8)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativeRarityIsRejectedSilently()
        {
            await new Spec("Cursed (negative-rarity) item cannot be rarified")
                .Given(WearableAtRarity_, (sbyte)-1)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedWithoutScrollIsRejected()
        {
            await new Spec("Protected rarify with no scroll in inventory rejects with InfoiPacket")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(CellonInInventoryNoScroll)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(SingleRejectionPacketShouldBeReturned)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void WearableAtRarity_(sbyte rare)
        {
            var item = new Item { VNum = ArmorVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor };
            var wearable = new WearableInstance(item, new Mock<Serilog.ILogger>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Rare = rare,
            };
            _wearable = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            _wearable.Slot = 0;
            _wearable.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_wearable.ItemInstanceId] = _wearable;
        }

        private void MaterialsInInventory()
        {
            AddStack(CellonVNum, 100, slot: 0);
            AddStack(ScrollVNum, 5, slot: 1);
        }

        private void CellonInInventoryNoScroll() => AddStack(CellonVNum, 100, slot: 0);

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

        private void NextRollWillBe_(double roll) => _random.Setup(r => r.NextDouble()).Returns(roll);

        // --- Whens ---

        private async Task UnprotectedRarifyIsExecuted() => _result = await _unprotected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.RarifyItem));

        private async Task ProtectedRarifyIsExecuted() => _result = await _protected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.RarifyItemProtected));

        private static UpgradePacket BuildPacket(UpgradePacketType type) => new()
        {
            UpgradeType = type,
            InventoryType = PocketType.Equipment,
            Slot = 0,
        };

        // --- Thens ---

        private void WearableRarityShouldBe_(sbyte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).Rare);

        private void CellonRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(CellonVNum));

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
