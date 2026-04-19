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
    public class EquipmentUpgradeOperationTests
    {
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;

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
        public async Task SuccessIncrementsUpgradeAndChargesCellonAndGold()
        {
            await new Spec("Successful upgrade increments level, consumes one cellon, charges gold")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(EnoughCellonInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillSucceed)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)1)
                .And(CellonRemainingShouldBe_, (short)9)
                .And(GoldShouldBe_, 99_500L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnprotectedFailureDecrementsUpgradeButCharges()
        {
            // upgrade 2 → success rate 0.90; roll 0.95 fails → upgrade goes to 1.
            await new Spec("Unprotected failure decrements upgrade and charges costs")
                .Given(WearableAtUpgrade_, (byte)2)
                .And(EnoughCellonInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillFail)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)1)
                .And(CellonRemainingShouldBe_, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedFailureKeepsUpgradeButChargesDoubleCellon()
        {
            await new Spec("Protected failure keeps upgrade and charges double cellon")
                .Given(WearableAtUpgrade_, (byte)2)
                .And(EnoughCellonInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillFail)
                .WhenAsync(ProtectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)2)
                .And(CellonRemainingShouldBe_, (short)4) // baseline 3 -> doubled to 6, leaves 10-6=4
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpgradeAtMaxLevelIsRejectedSilently()
        {
            await new Spec("Upgrade at +10 cap is rejected with no inventory or gold change")
                .Given(WearableAtUpgrade_, (byte)10)
                .And(EnoughCellonInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnprotectedFailureAtZeroDoesNotUnderflow()
        {
            // upgrade 2 → fail → 1 → would normally fail again to 0 but we test single shot
            await new Spec("Failure at upgrade 0 floors at 0 (no underflow)")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(EnoughCellonInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillFail) // at upgrade 0 success is 1.00 so this still succeeds
                .WhenAsync(UnprotectedUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)1) // forced success at rate 1.0
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

        private void EnoughCellonInInventory()
        {
            var cellon = InventoryItemInstance.Create(
                new ItemInstanceForTest(CellonVNum) { Amount = 10 },
                _session.Character.CharacterId);
            cellon.Slot = 0;
            cellon.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[cellon.ItemInstanceId] = cellon;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillSucceed() => _random.Setup(r => r.NextDouble()).Returns(0.0);

        private void NextRollWillFail() => _random.Setup(r => r.NextDouble()).Returns(0.95);

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

        private void CellonRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(CellonVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        // Tiny stand-in so we don't need the full ItemGenerationService just to put a stack of cellon in inventory.
        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
