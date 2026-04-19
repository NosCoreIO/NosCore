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
    [TestClass]
    public class RarifyOperationTests
    {
        private const short ArmorVNum = 1;
        private const short RedStellarVNum = 1024;
        private const short BlueStellarVNum = 1025;

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
        public async Task SuccessIncrementsRarityAndChargesRedStellarAndGold()
        {
            await new Spec("Successful rarify increments rarity, consumes red stellar, charges gold")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(StellarsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillSucceed)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)1)
                .And(RedStellarRemainingShouldBe_, (short)1)
                .And(BlueStellarRemainingShouldBe_, (short)2)
                .And(GoldShouldBe_, 95_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnprotectedFailureResetsRarityToZero()
        {
            // rarity 3 -> success rate 0.50; roll 0.95 fails -> rare reset to 0.
            await new Spec("Unprotected failure resets rare to 0")
                .Given(WearableAtRarity_, (sbyte)3)
                .And(StellarsInInventory)
                .And(CharacterHasGold_, 200_000L)
                .And(NextRollWillFail)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)0)
                .And(RedStellarRemainingShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedFailureKeepsRarityButConsumesBlueStellar()
        {
            await new Spec("Protected failure keeps rarity and consumes one blue stellar")
                .Given(WearableAtRarity_, (sbyte)3)
                .And(StellarsInInventory)
                .And(CharacterHasGold_, 200_000L)
                .And(NextRollWillFail)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)3)
                .And(RedStellarRemainingShouldBe_, (short)2)
                .And(BlueStellarRemainingShouldBe_, (short)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RarityAtMaxIsRejectedSilently()
        {
            await new Spec("Rarify at +8 cap is rejected with no charges")
                .Given(WearableAtRarity_, (sbyte)8)
                .And(StellarsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativeRarityIsRejectedSilently()
        {
            // Items can have negative rarity (cursed) — those aren't rarifiable.
            await new Spec("Rarify on negative-rarity item is rejected")
                .Given(WearableAtRarity_, (sbyte)-1)
                .And(StellarsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
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

        private void StellarsInInventory()
        {
            var red = InventoryItemInstance.Create(
                new ItemInstanceForTest(RedStellarVNum) { Amount = 2 },
                _session.Character.CharacterId);
            red.Slot = 0;
            red.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[red.ItemInstanceId] = red;

            var blue = InventoryItemInstance.Create(
                new ItemInstanceForTest(BlueStellarVNum) { Amount = 2 },
                _session.Character.CharacterId);
            blue.Slot = 1;
            blue.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[blue.ItemInstanceId] = blue;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillSucceed() => _random.Setup(r => r.NextDouble()).Returns(0.0);

        private void NextRollWillFail() => _random.Setup(r => r.NextDouble()).Returns(0.95);

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

        private void RedStellarRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(RedStellarVNum));

        private void BlueStellarRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(BlueStellarVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        // Tiny stand-in so we don't need the full ItemGenerationService just to put a stack of stellars in inventory.
        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
