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
            await new Spec("Roll inside the rare7 band rarifies up to 7")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(CellonRemainingShouldBe_, (short)95)
                .And(GoldShouldBe_, 99_500L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RollOutsideAllBandsDestroysTheItem()
        {
            await new Spec("Roll that matches no rare band destroys the wearable (unprotected)")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollAbsorbsNoBandMatchAndKeepsOriginalRare()
        {
            await new Spec("Protected scroll absorbs a no-band-match roll; item kept at original rare")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)5)
                .And(WearableShouldNotBeFixed)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollSkipsBandsThatWouldDowngrade()
        {
            await new Spec("Protected scroll allows improvement bands (rare7 with originalRare=5)")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessResetsRarityDrivenStatsViaSetRarityPoint()
        {
            await new Spec("Successful rarify re-rolls the rarity-driven stats via SetRarityPoint")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(SourceWearableHasPreExistingDefenseStats)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(CloseDefenceShouldBe_, (short)0)
                .And(DistanceDefenceShouldBe_, (short)0)
                .And(MagicDefenceShouldBe_, (short)0)
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
            var item = new Item
            {
                VNum = ArmorVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Armor,
                EquipmentSlot = EquipmentType.Armor,
            };
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Rare = rare,
            };
            _wearable = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            _wearable.Slot = 0;
            _wearable.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_wearable.ItemInstanceId] = _wearable;
        }

        private void SourceWearableHasPreExistingDefenseStats()
        {
            var wearable = (WearableInstance)_wearable.ItemInstance;
            wearable.CloseDefence = 50;
            wearable.DistanceDefence = 50;
            wearable.MagicDefence = 50;
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

        private void WearableShouldNotBeFixed() =>
            Assert.AreNotEqual(true, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).IsFixed);

        private void SourceSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));

        private void CloseDefenceShouldBe_(short expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).CloseDefence);

        private void DistanceDefenceShouldBe_(short expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).DistanceDefence);

        private void MagicDefenceShouldBe_(short expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).MagicDefence);

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
