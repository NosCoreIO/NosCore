//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.SpecialistHolderService
{
    [TestClass]
    public class SpecialistHolderServiceTests : SpecBase
    {
        private const short CardVNum = 912;
        private const short HolderVNum = 4500;
        private const short PetBeadVNum = 4501;
        private const short CardSlot = 0;
        private const byte HolderSlot = 1;

        private GameObject.Services.SpecialistHolderService.SpecialistHolderService _service = null!;
        private SpecialistStoreResult _result;
        private SpecialistHolderHandler _takeOut = null!;
        private InventoryItemInstance _holder = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = HolderVNum, ItemType = ItemType.Box, ItemSubType = 2,
                Effect = ItemEffectType.Holder, EffectValue = (int)HolderEffectValue.SpecialistCard
            });
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = PetBeadVNum, ItemType = ItemType.Box, ItemSubType = 0
            });
            await base.SetupAsync();
            _service = new GameObject.Services.SpecialistHolderService.SpecialistHolderService(ItemProvider);
            _takeOut = new SpecialistHolderHandler(_service);
        }

        [TestMethod]
        public async Task StoringACardKeepsItsUpgradePerfectionAndPoints()
        {
            await new Spec("Dropping a trained card on an empty holder moves all its progress into the holder")
                .Given(ACardInTheBag)
                .And(AnEmptyHolder)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheCardIsGone)
                .And(TheHolderKeepsTheCard)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.Stored)
                .And(TheResultShouldNameTheCardAndHolderSlots)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TakingTheCardOutGivesItBackUnchanged()
        {
            await new Spec("Using the filled holder gives back the card as it went in and uses up the holder")
                .Given(ACardInTheBag)
                .And(AnEmptyHolder)
                .And(TheCardIsDroppedOnTheHolder)
                .WhenAsync(TheHolderIsUsed)
                .Then(TheHolderIsGone)
                .And(TheHolderSlotIsCleared)
                .And(TheCardCameBackUnchanged)
                .And(TheClientIsTold_, Game18NConstString.SpecialistOutOfHolder)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWornCardIsRefused()
        {
            await new Spec("A card the character is wearing cannot be put in a holder")
                .Given(AWornCard)
                .And(AnEmptyHolder)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheHolderIsStillEmpty)
                .And(TheWornCardStays)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.Ignored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullHolderIsRefused()
        {
            await new Spec("A holder that already keeps a card does not take a second one")
                .Given(ACardInTheBag)
                .And(AHolderKeepingAnotherCard)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(TheHolderStillKeepsTheOtherCard)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.Ignored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherKindOfHolderIsRefused()
        {
            await new Spec("A pet bead does not take a specialist card")
                .Given(ACardInTheBag)
                .And(APetBead)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.Ignored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardCannotBeStoredWhileTransformed()
        {
            await new Spec("While transformed the holder refuses cards")
                .Given(ACardInTheBag)
                .And(AnEmptyHolder)
                .And(TheCharacterIsTransformed)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(TheHolderIsStillEmpty)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.Transformed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardWithADestroyedSoulIsRefused()
        {
            await new Spec("A card whose soul was destroyed cannot be stored")
                .Given(ADestroyedCardInTheBag)
                .And(AnEmptyHolder)
                .When(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(TheHolderIsStillEmpty)
                .And(TheOutcomeShouldBe_, SpecialistStoreOutcome.CannotStore)
                .ExecuteAsync();
        }

        private SpecialistInstance PlaceCard(NoscorePocketType pocket, short slot)
        {
            var card = (SpecialistInstance)ItemProvider.Create(CardVNum, 1, 0, 12);
            card.SpLevel = 80;
            card.Xp = 123_456;
            card.SpStoneUpgrade = 37;
            card.SpDamage = 10;
            card.SpDefence = 9;
            card.SpElement = 8;
            card.SpHp = 7;
            card.SpFire = 6;
            card.SpWater = 5;
            card.SpLight = 4;
            card.SpDark = 3;
            card.SlDamage = 40;
            card.SlDefence = 30;
            card.SlElement = 20;
            card.SlHp = 10;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(card, Session.Character.CharacterId), pocket, slot);
            return card;
        }

        private void ACardInTheBag() => PlaceCard(NoscorePocketType.Equipment, CardSlot);

        private void ADestroyedCardInTheBag() =>
            PlaceCard(NoscorePocketType.Equipment, CardSlot).Rare = SpUpgradeOperationBase.DestroyedSoulRare;

        private void AWornCard() => PlaceCard(NoscorePocketType.Wear, (short)EquipmentType.Sp);

        private void PlaceHolder(short vnum)
        {
            _holder = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(vnum, 1), Session.Character.CharacterId),
                NoscorePocketType.Equipment, HolderSlot)!.Single();
        }

        private void AnEmptyHolder() => PlaceHolder(HolderVNum);

        private void APetBead() => PlaceHolder(PetBeadVNum);

        private void AHolderKeepingAnotherCard()
        {
            PlaceHolder(HolderVNum);
            Holder.HoldingVNum = 901;
            Holder.Upgrade = 3;
        }

        private void TheCharacterIsTransformed() => Session.Character.UseSp = true;

        private BoxInstance Holder => (BoxInstance)_holder.ItemInstance;

        private void TheCardIsDroppedOnTheHolder() =>
            _result = _service.StoreFromEquipment(Session.Character, CardSlot, HolderSlot);

        private Task TheHolderIsUsed() =>
            _takeOut.Handle(new ItemUsedEvent(Session, _holder, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = PocketType.Equipment,
                Slot = HolderSlot,
            }));

        private InventoryItemInstance? CardIn(NoscorePocketType pocket) =>
            Session.Character.InventoryService.Values.FirstOrDefault(i => i.Type == pocket && i.ItemInstance.ItemVNum == CardVNum);

        private void TheCardIsGone() => Assert.IsNull(CardIn(NoscorePocketType.Equipment));

        private void TheCardStaysInTheBag() => Assert.IsNotNull(CardIn(NoscorePocketType.Equipment));

        private void TheWornCardStays() => Assert.IsNotNull(CardIn(NoscorePocketType.Wear));

        private void TheHolderIsStillEmpty() => Assert.IsTrue((Holder.HoldingVNum ?? 0) == 0);

        private void TheHolderStillKeepsTheOtherCard()
        {
            Assert.AreEqual((short)901, Holder.HoldingVNum);
            Assert.AreEqual((byte)3, Holder.Upgrade);
        }

        private void TheHolderIsGone() =>
            Assert.IsFalse(Session.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == HolderVNum));

        private void TheHolderKeepsTheCard()
        {
            Assert.AreEqual(CardVNum, Holder.HoldingVNum);
            Assert.AreEqual((byte)12, Holder.Upgrade);
            Assert.AreEqual((byte)80, Holder.SpLevel);
            Assert.AreEqual((byte)37, Holder.SpStoneUpgrade);
            Assert.AreEqual((short)40, Holder.SlDamage);
        }

        private void TheCardCameBackUnchanged()
        {
            var card = (SpecialistInstance)CardIn(NoscorePocketType.Equipment)!.ItemInstance;
            Assert.AreEqual((byte)12, card.Upgrade);
            Assert.AreEqual((byte)80, card.SpLevel);
            Assert.AreEqual(123_456L, card.Xp);
            Assert.AreEqual((byte)37, card.SpStoneUpgrade);
            CollectionAssert.AreEqual(new byte[] { 10, 9, 8, 7, 6, 5, 4, 3 },
                new[] { card.SpDamage, card.SpDefence, card.SpElement, card.SpHp, card.SpFire, card.SpWater, card.SpLight, card.SpDark });
            CollectionAssert.AreEqual(new short[] { 40, 30, 20, 10 },
                new[] { card.SlDamage, card.SlDefence, card.SlElement, card.SlHp });
        }

        private IvnSubPacket LastRedrawOf(short slot) =>
            Session.LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets!).Last(i => i.Slot == slot);

        private void TheOutcomeShouldBe_(SpecialistStoreOutcome outcome) => Assert.AreEqual(outcome, _result.Outcome);

        private void TheResultShouldNameTheCardAndHolderSlots()
        {
            Assert.AreEqual(CardSlot, _result.Card!.Slot);
            Assert.IsNull(_result.Remaining);
            Assert.AreEqual((short)HolderSlot, _result.Holder!.Slot);
        }

        private void TheHolderSlotIsCleared() => Assert.AreNotEqual(HolderVNum, LastRedrawOf(HolderSlot).VNum);

        private void TheClientIsTold_(Game18NConstString message) =>
            Assert.IsTrue(Session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));
    }
}
