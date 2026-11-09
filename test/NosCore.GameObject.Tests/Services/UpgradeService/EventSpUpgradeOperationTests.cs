//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.SpUpgradeService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class EventSpUpgradeOperationTests : SpecBase
    {
        private const short ScrollSlot = 0;

        private Mock<IRandomNumberSource> _random = null!;
        private EventSpUpgradeOperationBase[] _operations = null!;
        private SpecialistInstance _card = null!;
        private IReadOnlyList<IPacket> _result = Array.Empty<IPacket>();

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            _random = new Mock<IRandomNumberSource>();
            var localizer = TestHelpers.Instance.GameLanguageLocalizer;
            _operations = new EventSpUpgradeOperationBase[]
            {
                new ChickenSpUpgradeOperation(_random.Object, localizer, new SpUpgradeService()),
                new PyjamaSpUpgradeOperation(_random.Object, localizer, new SpUpgradeService()),
                new PirateSpUpgradeOperation(_random.Object, localizer, new SpUpgradeService()),
            };
            CharacterHasGold(1_000);
        }

        [TestMethod]
        public async Task ASuccessRaisesThePyjamaCardForOneScroll()
        {
            await new Spec("A successful roll raises the Pyjama card by one and spends one scroll and nothing else")
                .Given(ACard_At_, PyjamaSpUpgradeOperation.PyjamaCardVNum, (byte)0)
                .And(Scrolls_, PyjamaSpUpgradeOperation.PyjamaScrollVNum, (short)2)
                .And(TheRollIs_, 0.99)
                .WhenAsync(UpgradingIn_, UpgradePacketType.UpgradeSpPyjama)
                .Then(TheCardShouldBeAt_, (byte)1)
                .And(ScrollsLeftShouldBe_, PyjamaSpUpgradeOperation.PyjamaScrollVNum, 1)
                .And(GoldShouldBe, 1_000L)
                .And(TheMessageShouldBe_, Game18NConstString.UpgradeSuccessful)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRollKeepsTheChickenCardAndSpendsTheScroll()
        {
            await new Spec("A failed roll leaves the Chicken card as it was but still spends the scroll")
                .Given(ACard_At_, ChickenSpUpgradeOperation.ChickenCardVNum, (byte)0)
                .And(Scrolls_, ChickenSpUpgradeOperation.ChickenScrollVNum, (short)1)
                .And(TheRollIs_, 0.10)
                .WhenAsync(UpgradingIn_, UpgradePacketType.UpgradeSpChiken)
                .Then(TheCardShouldBeAt_, (byte)0)
                .And(TheCardShouldNotBeDestroyed)
                .And(ScrollsLeftShouldBe_, ChickenSpUpgradeOperation.ChickenScrollVNum, 0)
                .And(TheMessageShouldBe_, Game18NConstString.SpecialistUpgradeFailed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheEventPirateCardIsUpgradedToo()
        {
            await new Spec("The event copy of the Pirate card is upgraded with the Pirate scroll")
                .Given(ACard_At_, PirateSpUpgradeOperation.EventPirateCardVNum, (byte)3)
                .And(Scrolls_, PirateSpUpgradeOperation.PirateScrollVNum, (short)1)
                .And(TheRollIs_, 0.99)
                .WhenAsync(UpgradingIn_, UpgradePacketType.UpgradeSpPirate)
                .Then(TheCardShouldBeAt_, (byte)4)
                .And(ScrollsLeftShouldBe_, PirateSpUpgradeOperation.PirateScrollVNum, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutTheScrollNothingHappens()
        {
            await new Spec("Without the card's own scroll the upgrade is refused and the card is left alone")
                .Given(ACard_At_, PyjamaSpUpgradeOperation.PyjamaCardVNum, (byte)0)
                .And(Scrolls_, ChickenSpUpgradeOperation.ChickenScrollVNum, (short)1)
                .And(TheRollIs_, 0.99)
                .WhenAsync(UpgradingIn_, UpgradePacketType.UpgradeSpPyjama)
                .Then(TheRefusalShouldBe_, Game18NConstString.NotEnoughIngredients)
                .And(TheCardShouldBeAt_, (byte)0)
                .And(ScrollsLeftShouldBe_, ChickenSpUpgradeOperation.ChickenScrollVNum, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherCardCannotBeUpgradedInTheWindow()
        {
            await new Spec("A card that is not the window's own is refused and the scroll is kept")
                .Given(ACard_At_, ChickenSpUpgradeOperation.ChickenCardVNum, (byte)0)
                .And(Scrolls_, PyjamaSpUpgradeOperation.PyjamaScrollVNum, (short)1)
                .And(TheRollIs_, 0.99)
                .WhenAsync(UpgradingIn_, UpgradePacketType.UpgradeSpPyjama)
                .Then(TheRefusalShouldBe_, Game18NConstString.CardCannotBeUpgraded)
                .And(TheCardShouldBeAt_, (byte)0)
                .And(ScrollsLeftShouldBe_, PyjamaSpUpgradeOperation.PyjamaScrollVNum, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingTheScrollOpensItsWindow()
        {
            await new Spec("Using the Pirate scroll opens the Pirate upgrade window and keeps the scroll")
                .Given(Scrolls_, PirateSpUpgradeOperation.PirateScrollVNum, (short)1)
                .WhenAsync(UsingTheScrollIn_, ScrollSlot)
                .Then(TheWindowShouldBe_, WindowType.UpgradeSpPirate)
                .And(TheScrollWindowShouldBeOpen)
                .And(ScrollsLeftShouldBe_, PirateSpUpgradeOperation.PirateScrollVNum, 1)
                .ExecuteAsync();
        }

        private void ACard_At_(short vnum, byte upgrade)
        {
            _card = new SpecialistInstance(new Item
            {
                VNum = vnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, Morph = 12,
            })
            {
                Amount = 1,
                Upgrade = upgrade,
                SpLevel = 99,
            };
            var inst = InventoryItemInstance.Create(_card, Session.Character.CharacterId);
            inst.Slot = 0;
            inst.Type = NoscorePocketType.Equipment;
            Session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void Scrolls_(short vnum, short amount)
        {
            var inst = InventoryItemInstance.Create(
                new ItemInstance(new Item { VNum = vnum, Type = NoscorePocketType.Main }) { Amount = amount },
                Session.Character.CharacterId);
            inst.Slot = ScrollSlot;
            inst.Type = NoscorePocketType.Main;
            Session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void TheRollIs_(double value) => _random.Setup(r => r.NextDouble()).Returns(value);

        private async Task UpgradingIn_(UpgradePacketType kind)
        {
            var operation = _operations.Single(o => o.Kind == kind);
            _result = await operation.ExecuteAsync(Session, new UpgradePacket
            {
                UpgradeType = kind,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            });
        }

        private Task UsingTheScrollIn_(short slot)
        {
            var scroll = Session.Character.InventoryService.LoadBySlotAndType(slot, NoscorePocketType.Main)!;
            return new UpgradeWindowItemHandler(_operations, TestHelpers.Instance.NpcDialogService)
                .Handle(new ItemUsedEvent(Session, scroll, new UseItemPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = Session.Character.CharacterId,
                    Type = PocketType.Main,
                    Slot = slot,
                }));
        }

        private void TheCardShouldBeAt_(byte upgrade) => Assert.AreEqual(upgrade, _card.Upgrade);

        private void TheCardShouldNotBeDestroyed() =>
            Assert.AreNotEqual(SpUpgradeOperationBase.DestroyedSoulRare, _card.Rare);

        private void ScrollsLeftShouldBe_(short vnum, int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(vnum));

        private void TheMessageShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<MsgiPacket>().Single().Message);

        private void TheRefusalShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<SayiPacket>().Single().Message);

        private void TheWindowShouldBe_(WindowType window) =>
            Assert.AreEqual(window, GetLastPacket<WopenPacket>()!.Type);

        private void TheScrollWindowShouldBeOpen() =>
            Assert.IsTrue(TestHelpers.Instance.NpcDialogService.IsOpen(Session.Character, NpcDialogWindow.UpgradeScroll));
    }
}
