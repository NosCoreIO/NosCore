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
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using Mate = NosCore.GameObject.Services.MateService.Mate;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class PartnerSpecialistHolderTests : SpecBase
    {
        private const short CardVNum = 4100;
        private const short HolderVNum = 4801;
        private const short HolderSlot = 1;

        private SpecialistHolderHandler _handler;
        private InventoryItemInstance _holder;
        private Mate _partner;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = HolderVNum, ItemType = ItemType.Box, ItemSubType = 6,
                Effect = (ItemEffectType)BoxItemEffectType.NosMateHolder
            });
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = CardVNum, ItemType = ItemType.Specialist, ItemSubType = 4,
                EquipmentSlot = EquipmentType.Sp
            });
            await base.SetupAsync();
            World.Inject<IItemGenerationService>(ItemProvider);
            World.UseReal<ISpecialistHolderService, SpecialistHolderService>();
            _handler = World.Create<SpecialistHolderHandler>();
        }

        [TestMethod]
        public async Task TheHolderAsksBeforeTakingThePartnersCard()
        {
            await new Spec("An empty partner holder asks before taking the selected partner's card")
                .Given(APartnerWearingACard)
                .And(AnEmptyHolder)
                .WhenAsync(TheHolderIsUsedOnThePartner_, (short)0)
                .Then(TheQuestionTargetsThePartner)
                .And(ThePartnerStillWearsTheCard)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingMovesTheCardIntoTheHolder()
        {
            await new Spec("Confirming moves the partner's card and its progress into the holder")
                .Given(APartnerWearingACard)
                .And(AnEmptyHolder)
                .WhenAsync(TheHolderIsUsedOnThePartner_, (short)1)
                .Then(TheHolderKeepsTheCard)
                .And(ThePartnerWearsNoCard)
                .And(ShouldReceiveMessage, Game18NConstString.SpecialistInTheHolder)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATransformedPartnerKeepsItsCard()
        {
            await new Spec("A transformed partner keeps its card")
                .Given(APartnerWearingACard)
                .And(ThePartnerIsTransformed)
                .And(AnEmptyHolder)
                .WhenAsync(TheHolderIsUsedOnThePartner_, (short)1)
                .Then(ShouldReceiveMessage, Game18NConstString.CantPutSpecialistInHolderWhileTransformed)
                .And(ThePartnerStillWearsTheCard)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerWithoutACardIsRefused()
        {
            await new Spec("A partner without a card is refused")
                .Given(APartnerWithoutACard)
                .And(AnEmptyHolder)
                .WhenAsync(TheHolderIsUsedOnThePartner_, (short)1)
                .Then(ShouldReceiveMessage, Game18NConstString.NoSpecialistCardEquipped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFilledHolderGivesTheCardBack()
        {
            await new Spec("A filled partner holder gives the card back to the bag and disappears")
                .Given(APartnerWearingACard)
                .And(AnEmptyHolder)
                .AndAsync(TheHolderIsUsedOnThePartner_, (short)1)
                .WhenAsync(TheHolderIsUsedOnThePartner_, (short)0)
                .Then(TheCardIsInTheBag)
                .And(TheHolderIsGone)
                .And(ShouldReceiveMessage, Game18NConstString.SpecialistOutOfHolder)
                .ExecuteAsync();
        }

        private void APartnerWithoutACard()
        {
            _partner = TestMates.Partner(Session.Character.CharacterId);
            Session.Character.Mates[_partner.MateTransportId] = _partner;
        }

        private void APartnerWearingACard()
        {
            APartnerWithoutACard();
            var card = (SpecialistInstance)ItemProvider.Create(CardVNum, 1);
            card.SpLevel = 40;
            card.Upgrade = 3;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(card, Session.Character.CharacterId),
                NoscorePocketType.FirstPartnerInventory, (short)EquipmentType.Sp);
        }

        private void ThePartnerIsTransformed() => _partner.SpecialistMorph = 2300;

        private void AnEmptyHolder() =>
            _holder = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(HolderVNum, 1), Session.Character.CharacterId),
                NoscorePocketType.Equipment, HolderSlot).NotNull().Single();

        private Task TheHolderIsUsedOnThePartner_(short mode)
        {
            Session.LastPackets.Clear();
            return _handler.Handle(new ItemUsedEvent(Session, _holder, new UseItemPacket
            {
                VisualType = VisualType.Npc,
                VisualId = _partner.MateTransportId,
                Type = PocketType.Equipment,
                Slot = HolderSlot,
                Mode = mode
            }));
        }

        private void TheQuestionTargetsThePartner()
        {
            var question = GetLastPacket<QnaiPacket>().NotNull();
            Assert.AreEqual(Game18NConstString.AskPutSpecialistInHolder, question.Question);
            var yes = (UseItemPacket)question.YesPacket.NotNull();
            Assert.AreEqual(_partner.MateTransportId, yes.VisualId);
            Assert.AreEqual(1, yes.Mode);
        }

        private SpecialistInstance? WornCard() =>
            Session.Character.InventoryService.LoadBySlotAndType(EquipmentType.Sp, NoscorePocketType.FirstPartnerInventory)
                ?.ItemInstance as SpecialistInstance;

        private void ThePartnerStillWearsTheCard() => Assert.IsNotNull(WornCard());

        private void ThePartnerWearsNoCard() => Assert.IsNull(WornCard());

        private void TheHolderKeepsTheCard()
        {
            var holder = (BoxInstance)_holder.ItemInstance;
            Assert.AreEqual(CardVNum, holder.HoldingVNum);
            Assert.AreEqual((byte)40, holder.SpLevel);
            Assert.AreEqual((byte)3, holder.Upgrade);
        }

        private void TheCardIsInTheBag()
        {
            var card = Session.Character.InventoryService.Values
                .Where(i => i.Type == NoscorePocketType.Equipment)
                .Select(i => i.ItemInstance).OfType<SpecialistInstance>().Single(i => i.ItemVNum == CardVNum);
            Assert.AreEqual((byte)40, card.SpLevel);
        }

        private void TheHolderIsGone() =>
            Assert.IsFalse(Session.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == HolderVNum));
    }
}
