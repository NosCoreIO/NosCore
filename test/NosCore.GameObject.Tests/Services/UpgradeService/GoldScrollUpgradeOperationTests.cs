//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.EquipmentUpgradeService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class GoldScrollUpgradeOperationTests
    {
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;
        private const short SmallGemVNum = 1015;
        private const short FullGemVNum = 1016;
        private const short ScrollVNum = 1218;
        private const short GoldScrollVNum = 5369;

        private ClientSession _session = null!;
        private Mock<IRandomNumberSource> _random = null!;
        private IUpgradeOperation _operation = null!;
        private IReadOnlyList<IPacket>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _random = new Mock<IRandomNumberSource>();
            _operation = new UpgradeItemGoldScrollOperation(_random.Object,
                TestHelpers.Instance.GameLanguageLocalizer, new EquipmentUpgradeService());
        }

        [TestMethod]
        public void TheGoldScrollUpgradeAnswersItsOwnPacketType() =>
            new Spec("The gold scroll upgrade is dispatched for up_gr 43")
                .Then(OperationKindShouldBeGoldScroll)
                .Execute();

        [TestMethod]
        public async Task AFailedRollIsAbsorbedByTheGoldScroll()
        {
            await new Spec("A failing roll keeps the item and spends a gold scroll, not a plain one")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsAndBothScrolls)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(GoldScrollUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)5)
                .And(GoldScrollsRemainingShouldBe_, 4)
                .And(PlainScrollsRemainingShouldBe_, 5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASuccessfulRollRaisesTheItemAndSpendsTheGoldScroll()
        {
            await new Spec("A successful roll raises the item by one and spends a gold scroll")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(MaterialsAndBothScrolls)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.99)
                .WhenAsync(GoldScrollUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)1)
                .And(GoldScrollsRemainingShouldBe_, 4)
                .And(PlainScrollsRemainingShouldBe_, 5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheGoldScrollUpgradeChargesHalfTheGoldAndCella()
        {
            await new Spec("A +5 upgrade costs 40 000 gold and 110 cella instead of 80 000 and 220")
                .Given(WearableAtUpgrade_, (byte)5)
                .And(MaterialsAndBothScrolls)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.99)
                .WhenAsync(GoldScrollUpgradeIsExecuted)
                .Then(GoldShouldBe_, 960_000L)
                .And(CellaRemainingShouldBe_, 890)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APlainScrollDoesNotStandInForTheGoldScroll()
        {
            await new Spec("Without a gold scroll the upgrade is refused even with plain scrolls")
                .Given(WearableAtUpgrade_, (byte)0)
                .And(MaterialsAndPlainScrollsOnly)
                .And(CharacterHasGold_, 1_000_000L)
                .And(NextRollWillBe_, 0.99)
                .WhenAsync(GoldScrollUpgradeIsExecuted)
                .Then(WearableUpgradeShouldBe_, (byte)0)
                .And(MissingIngredientsShouldBeReported)
                .And(PlainScrollsRemainingShouldBe_, 5)
                .ExecuteAsync();
        }

        private void WearableAtUpgrade_(byte upgrade)
        {
            var item = new Item { VNum = ArmorVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor };
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                TestHelpers.Instance.LogLanguageLocalizer)
            {
                Upgrade = upgrade,
            };
            var instance = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            instance.Slot = 0;
            instance.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[instance.ItemInstanceId] = instance;
        }

        private void MaterialsAndBothScrolls()
        {
            MaterialsAndPlainScrollsOnly();
            AddStack(GoldScrollVNum, 5, slot: 4);
        }

        private void MaterialsAndPlainScrollsOnly()
        {
            AddStack(CellonVNum, 1000, slot: 0);
            AddStack(SmallGemVNum, 10, slot: 1);
            AddStack(FullGemVNum, 10, slot: 2);
            AddStack(ScrollVNum, 5, slot: 3);
        }

        private void AddStack(short vnum, short amount, short slot)
        {
            var instance = InventoryItemInstance.Create(
                new ItemInstanceForTest(vnum) { Amount = amount },
                _session.Character.CharacterId);
            instance.Slot = slot;
            instance.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[instance.ItemInstanceId] = instance;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillBe_(double roll) => _random.Setup(r => r.NextDouble()).Returns(roll);

        private async Task GoldScrollUpgradeIsExecuted() => _result = await _operation.ExecuteAsync(_session,
            new UpgradePacket
            {
                UpgradeType = UpgradePacketType.UpgradeItemGoldScroll,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            });

        private void OperationKindShouldBeGoldScroll() =>
            Assert.AreEqual(UpgradePacketType.UpgradeItemGoldScroll, _operation.Kind);

        private void WearableUpgradeShouldBe_(byte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment)!.ItemInstance!).Upgrade);

        private void GoldScrollsRemainingShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(GoldScrollVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void CellaRemainingShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(CellonVNum));

        private void PlainScrollsRemainingShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(ScrollVNum));

        private void MissingIngredientsShouldBeReported() =>
            Assert.AreEqual(Game18NConstString.NotEnoughIngredients,
                _result?.OfType<InfoiPacket>().Single().Message);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
