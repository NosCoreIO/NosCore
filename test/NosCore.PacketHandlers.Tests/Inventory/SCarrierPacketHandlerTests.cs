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
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class SCarrierPacketHandlerTests : SpecBase
    {
        private const short CardVNum = 912;
        private const short HolderVNum = 4500;
        private const short PetBeadVNum = 4501;
        private const short CardSlot = 0;
        private const byte HolderSlot = 1;

        private SCarrierPacketHandler _handler = null!;
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
            var service = new SpecialistHolderService(ItemProvider);
            _handler = new SCarrierPacketHandler(service);
        }

        [TestMethod]
        public async Task StoringACardKeepsItsUpgradePerfectionAndPoints()
        {
            await new Spec("Dropping a trained card on an empty holder moves all its progress into the holder")
                .Given(ACardInTheBag)
                .And(AnEmptyHolder)
                .WhenAsync(TheCardIsDroppedOnTheHolder)
                .Then(TheCardIsGone)
                .And(TheCardSlotIsCleared)
                .And(TheHolderKeepsTheCard)
                .And(TheWindowClosesWith_, Game18NConstString.SpecialistInTheHolder)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherKindOfHolderIsRefused()
        {
            await new Spec("A pet bead does not take a specialist card")
                .Given(ACardInTheBag)
                .And(APetBead)
                .WhenAsync(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardWithADestroyedSoulIsRefused()
        {
            await new Spec("A card whose soul was destroyed cannot be stored")
                .Given(ADestroyedCardInTheBag)
                .And(AnEmptyHolder)
                .WhenAsync(TheCardIsDroppedOnTheHolder)
                .Then(TheCardStaysInTheBag)
                .And(TheHolderIsStillEmpty)
                .And(ShouldReceiveMessage, Game18NConstString.NotBeAbleToPutTheSelectedSpecialistCardIntoCardHolder)
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

        private void PlaceHolder(short vnum)
        {
            _holder = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(vnum, 1), Session.Character.CharacterId),
                NoscorePocketType.Equipment, HolderSlot)!.Single();
        }

        private void AnEmptyHolder() => PlaceHolder(HolderVNum);

        private void APetBead() => PlaceHolder(PetBeadVNum);

        private BoxInstance Holder => (BoxInstance)_holder.ItemInstance;

        private Task TheCardIsDroppedOnTheHolder() =>
            _handler.ExecuteAsync(new SCarrierPacket { Slot = CardSlot, HolderSlot = HolderSlot }, Session);

        private InventoryItemInstance? CardIn(NoscorePocketType pocket) =>
            Session.Character.InventoryService.Values.FirstOrDefault(i => i.Type == pocket && i.ItemInstance.ItemVNum == CardVNum);

        private void TheCardIsGone() => Assert.IsNull(CardIn(NoscorePocketType.Equipment));

        private void TheCardStaysInTheBag() => Assert.IsNotNull(CardIn(NoscorePocketType.Equipment));

        private void TheHolderIsStillEmpty() => Assert.IsTrue((Holder.HoldingVNum ?? 0) == 0);

        private void TheHolderKeepsTheCard()
        {
            Assert.AreEqual(CardVNum, Holder.HoldingVNum);
            Assert.AreEqual((byte)12, Holder.Upgrade);
            Assert.AreEqual((byte)80, Holder.SpLevel);
            Assert.AreEqual((byte)37, Holder.SpStoneUpgrade);
            Assert.AreEqual((short)40, Holder.SlDamage);
        }

        private IvnSubPacket LastRedrawOf(short slot) =>
            Session.LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets!).Last(i => i.Slot == slot);

        private void TheCardSlotIsCleared() => Assert.AreNotEqual(CardVNum, LastRedrawOf(CardSlot).VNum);

        private void TheWindowClosesWith_(Game18NConstString message)
        {
            Assert.AreEqual(ShopEndPacketType.CloseSubWindow, GetLastPacket<ShopEndPacket>()!.Type);
            ShouldReceiveMessage(message);
        }
    }
}
