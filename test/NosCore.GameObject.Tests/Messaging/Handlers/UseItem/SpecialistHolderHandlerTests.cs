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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class SpecialistHolderHandlerTests : SpecBase
    {
        private const short CardVNum = 912;
        private const short HolderVNum = 4500;
        private const short HolderSlot = 1;

        private SpecialistHolderHandler _handler = null!;
        private InventoryItemInstance _holder = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = HolderVNum, ItemType = ItemType.Box, ItemSubType = 2,
                Effect = ItemEffectType.Holder, EffectValue = (int)HolderEffectValue.GoldenSpecialistCard
            });
            await base.SetupAsync();
            _handler = new SpecialistHolderHandler(new SpecialistHolderService(ItemProvider));
        }

        [TestMethod]
        public async Task UsingAnEmptyHolderOpensTheCardWindow()
        {
            await new Spec("Using an empty holder opens the window the card is dropped into")
                .Given(AHolderKeeping_, (short)0)
                .WhenAsync(TheHolderIsUsed)
                .Then(TheHolderWindowOpens)
                .And(TheHolderIsStillInTheBag)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingAFilledHolderGivesTheCardBack()
        {
            await new Spec("Using a filled holder gives back the card with its upgrade and points")
                .Given(AHolderKeeping_, CardVNum)
                .WhenAsync(TheHolderIsUsed)
                .Then(TheCardIsBackWithItsProgress)
                .And(TheHolderIsGone)
                .And(ShouldReceiveMessage, Game18NConstString.SpecialistOutOfHolder)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullBagKeepsTheCardInTheHolder()
        {
            await new Spec("With no free slot the card stays in the holder")
                .Given(AHolderKeeping_, CardVNum)
                .And(TheBagIsFull)
                .WhenAsync(TheHolderIsUsed)
                .Then(ShouldReceiveMessage, Game18NConstString.NotEnoughSpace)
                .And(TheHolderIsStillInTheBag)
                .And(NoCardWasCreated)
                .ExecuteAsync();
        }

        private void AHolderKeeping_(short vnum)
        {
            var holder = (BoxInstance)ItemProvider.Create(HolderVNum, 1);
            if (vnum != 0)
            {
                holder.HoldingVNum = vnum;
                holder.Upgrade = 9;
                holder.SpLevel = 65;
                holder.SpStoneUpgrade = 21;
                holder.SpDamage = 12;
                holder.SlDamage = 55;
            }

            _holder = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(holder, Session.Character.CharacterId),
                NoscorePocketType.Equipment, HolderSlot)!.Single();
        }

        private void TheBagIsFull()
        {
            for (short slot = 0; slot < TestHelpers.Instance.WorldConfiguration.Value.BackpackSize; slot++)
            {
                if (slot == HolderSlot)
                {
                    continue;
                }

                Session.Character.InventoryService.AddItemToPocket(
                    InventoryItemInstance.Create(ItemProvider.Create(1, 1), Session.Character.CharacterId),
                    NoscorePocketType.Equipment, slot);
            }
        }

        private Task TheHolderIsUsed() =>
            _handler.Handle(new ItemUsedEvent(Session, _holder, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = PocketType.Equipment,
                Slot = HolderSlot,
            }));

        private SpecialistInstance? Card() =>
            Session.Character.InventoryService.Values.Select(i => i.ItemInstance).OfType<SpecialistInstance>()
                .FirstOrDefault(i => i.ItemVNum == CardVNum);

        private void TheHolderWindowOpens()
        {
            var window = GetLastPacket<WopenPacket>();
            Assert.IsNotNull(window);
            Assert.AreEqual(WindowType.GoldenSPCardHolder, window.Type);
            Assert.AreEqual(HolderSlot, window.Unknown);
        }

        private void TheHolderIsStillInTheBag() =>
            Assert.IsTrue(Session.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == HolderVNum));

        private void TheHolderIsGone() =>
            Assert.IsFalse(Session.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == HolderVNum));

        private void NoCardWasCreated() => Assert.IsNull(Card());

        private void TheCardIsBackWithItsProgress()
        {
            var card = Card();
            Assert.IsNotNull(card);
            Assert.AreEqual((byte)9, card.Upgrade);
            Assert.AreEqual((byte)65, card.SpLevel);
            Assert.AreEqual((byte)21, card.SpStoneUpgrade);
            Assert.AreEqual((byte)12, card.SpDamage);
            Assert.AreEqual((short)55, card.SlDamage);
        }
    }
}
