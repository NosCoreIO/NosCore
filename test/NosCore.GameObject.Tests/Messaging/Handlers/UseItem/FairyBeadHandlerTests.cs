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
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Services.FairyBeadService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using ServerGuriPacket = NosCore.Packets.ServerPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class FairyBeadHandlerTests : SpecBase
    {
        private const short FairyBeadVNum = 4194;
        private const short FairyVNum = 274;
        private const short WeaponVNum = 1;
        private const short BeadSlot = 1;
        private const short FairySlot = 0;

        private InventoryItemInstance? _bead;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = FairyBeadVNum, ItemType = ItemType.Box, ItemSubType = 5,
                Effect = ItemEffectType.Holder, EffectValue = (int)HolderEffectValue.Fairy
            });
            DefaultItems.Add(new Item
            {
                Type = NoscorePocketType.Equipment, VNum = FairyVNum, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Fairy, ElementRate = 10
            });
            await base.SetupAsync();
            World.Inject<IItemGenerationService>(ItemProvider);
            World.UseReal<IFairyBeadService, FairyBeadService>();
        }

        [TestMethod]
        public async Task AnEmptyBeadAsksForAFairy()
        {
            await new Spec("Using an empty fairy bead opens the window the fairy is dropped into")
                .Given(ABeadKeeping_, (short)0)
                .WhenAsync(TheBeadIsUsed)
                .Then(TheServerSends_, GuriPacketType.AddFairyInPearl)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFairyGoesIntoTheBeadWithItsLevel()
        {
            await new Spec("Confirming the window moves the fairy and its level into the bead and closes the window")
                .Given(ABeadKeeping_, (short)0)
                .And(A_At_Percent, FairyVNum, (short)30)
                .WhenAsync(TheClientConfirms)
                .Then(TheBeadKeeps_, FairyVNum)
                .And(TheBeadKeepsElementRate_, (short)30)
                .And(No_IsInTheBag, FairyVNum)
                .And(TheServerSends_, GuriPacketType.CloseFairyPearlWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyAFairyGoesIntoTheBead()
        {
            await new Spec("A weapon in the fairy slot of the window stays in the bag")
                .Given(ABeadKeeping_, (short)0)
                .And(A_At_Percent, WeaponVNum, (short)0)
                .WhenAsync(TheClientConfirms)
                .Then(TheBeadKeeps_, (short)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullBeadTakesNoSecondFairy()
        {
            await new Spec("A bead that already keeps a fairy refuses another")
                .Given(ABeadKeeping_, FairyVNum)
                .And(A_At_Percent, FairyVNum, (short)50)
                .WhenAsync(TheClientConfirms)
                .Then(TheBeadKeepsElementRate_, (short)30)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullBeadGivesTheFairyBack()
        {
            await new Spec("Using a full fairy bead gives back the fairy at its level")
                .Given(ABeadKeeping_, FairyVNum)
                .WhenAsync(TheBeadIsUsed)
                .Then(TheFairyIsBackAt_Percent, (short)30)
                .And(TheBeadIsGone)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameBeadUsedTwiceGivesOneFairy()
        {
            await new Spec("The same full bead used twice gives back one fairy")
                .Given(ABeadKeeping_, FairyVNum)
                .WhenAsync(TheBeadIsUsedTwice)
                .Then(ExactlyOneFairyIsInTheBag)
                .ExecuteAsync();
        }

        private void ABeadKeeping_(short heldVNum)
        {
            var bead = (BoxInstance)ItemProvider.Create(FairyBeadVNum, 1);
            if (heldVNum != 0)
            {
                bead.HoldingVNum = heldVNum;
                bead.ElementRate = 30;
            }

            _bead = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(bead, Session.Character.CharacterId),
                NoscorePocketType.Equipment, BeadSlot).NotNull().Single();
        }

        private void A_At_Percent(short vnum, short elementRate)
        {
            var item = (WearableInstance)ItemProvider.Create(vnum, 1);
            item.ElementRate = elementRate;
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Equipment, FairySlot);
        }

        private Task TheBeadIsUsed() =>
            World.Create<FairyBeadHandler>().Handle(new ItemUsedEvent(Session, _bead.NotNull(), new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.CharacterId,
                Type = PocketType.Equipment,
                Slot = BeadSlot,
            }));

        private async Task TheBeadIsUsedTwice()
        {
            await TheBeadIsUsed();
            await TheBeadIsUsed();
        }

        private Task TheClientConfirms() =>
            World.Create<FairyBeadStoreHandler>().Handle(new GuriPacketReceivedEvent(Session, new ClientGuriPacket
            {
                Type = GuriPacketType.StoreFairyInPearl,
                Argument = 0,
                VisualId = BeadSlot,
                Data = (long)PocketType.Equipment,
                Value = FairySlot.ToString(),
            }));

        private void TheServerSends_(GuriPacketType type)
        {
            var guri = GetLastPacket<ServerGuriPacket>().NotNull();
            Assert.AreEqual(type, guri.Type);
            if (type == GuriPacketType.AddFairyInPearl)
            {
                Assert.AreEqual(BeadSlot, guri.EntityId);
            }
        }

        private BoxInstance Bead() => (BoxInstance)_bead.NotNull().ItemInstance;

        private void TheBeadKeeps_(short vnum) => Assert.AreEqual(vnum, Bead().HoldingVNum ?? 0);

        private void TheBeadKeepsElementRate_(short rate) => Assert.AreEqual(rate, Bead().ElementRate);

        private void No_IsInTheBag(short vnum) =>
            Assert.IsFalse(Session.Character.InventoryService.Values.Any(i => i.ItemInstance.ItemVNum == vnum));

        private void TheBeadIsGone() => No_IsInTheBag(FairyBeadVNum);

        private void ExactlyOneFairyIsInTheBag() =>
            Assert.AreEqual(1, Session.Character.InventoryService.Values.Count(i => i.ItemInstance.ItemVNum == FairyVNum));

        private void TheFairyIsBackAt_Percent(short rate)
        {
            var fairy = Session.Character.InventoryService.Values.Select(i => i.ItemInstance)
                .OfType<WearableInstance>().FirstOrDefault(i => i.ItemVNum == FairyVNum);
            Assert.IsNotNull(fairy);
            Assert.AreEqual(rate, fairy.ElementRate);
        }
    }
}
