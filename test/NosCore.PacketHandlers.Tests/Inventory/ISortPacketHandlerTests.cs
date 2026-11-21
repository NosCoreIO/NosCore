//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class ISortPacketHandlerTests : SpecBase
    {
        private ISortPacketHandler Handler;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Handler = World.Create<ISortPacketHandler>();
        }

        [TestMethod]
        public async Task TheSortIsAskedBeforeItIsDone()
        {
            await new Spec("The sort is asked before it is done")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Sorting_, PocketType.Main)
                .Then(TheSortOf_ShouldBeAsked, PocketType.Main)
                .And(ItemShouldBeInSlot_, 1)
                .And(NoPocketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnsweringYesSortsAndSendsThePocketAndTheQuicklist()
        {
            await new Spec("Answering yes sorts and sends the whole pocket and the quicklist")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Answering_For_, true, PocketType.Main)
                .Then(ItemShouldBeInSlot_, 0)
                .And(ThePocketShouldBeSentWithTheItemInSlot0)
                .And(TheQuicklistShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnsweringNoLeavesThePocket()
        {
            await new Spec("Answering no leaves the pocket as it was")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Answering_For_, false, PocketType.Main)
                .Then(ItemShouldBeInSlot_, 1)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ThePocketOfWornItemsIsNotSorted()
        {
            await new Spec("The pocket of worn items is neither asked nor sorted")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Answering_For_, true, PocketType.Wear)
                .Then(ItemShouldBeInSlot_, 1)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void MainPocketHasAnItemInItsLastSlot()
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(1012, 7), Session.Character.CharacterId),
                NoscorePocketType.Main, 1);
        }

        private async Task Sorting_(PocketType pocket)
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ISortPacket { PocketType = pocket }, Session);
        }

        private async Task Answering_For_(bool answer, PocketType pocket)
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ISortPacket { PocketType = pocket, Confirmed = answer }, Session);
        }

        private void TheSortOf_ShouldBeAsked(PocketType pocket) =>
            PacketAssert.AreEqual(new QnaiPacket
            {
                YesPacket = new ISortPacket { PocketType = pocket, Confirmed = true },
                Question = Game18NConstString.SortInventoryQuestion
            }, Session.LastPackets.OfType<QnaiPacket>().Single());

        private void ItemShouldBeInSlot_(int slot) =>
            Assert.AreEqual((short)1012,
                Session.Character.InventoryService.LoadBySlotAndType((short)slot, NoscorePocketType.Main)?.ItemInstance.ItemVNum);

        private void NoPocketShouldBeSent() =>
            Assert.IsFalse(Session.LastPackets.Any(p => p is InvPacket or IvnPacket));

        private void ThePocketShouldBeSentWithTheItemInSlot0() =>
            PacketAssert.AreEqual(new InvPacket
            {
                Type = PocketType.Main,
                IvnSubPackets = [new IvnSubPacket { Slot = 0, VNum = 1012, RareAmount = 7, UpgradeDesign = 0, SecondUpgrade = null, Unknown5 = null, Unknown6 = null }]
            }, Session.LastPackets.OfType<InvPacket>().Single());

        private void TheQuicklistShouldBeSent() =>
            CollectionAssert.AreEqual(new long[] { 0, 1 }, Session.LastPackets.OfType<QSlotPacket>().Select(p => p.Slot).ToArray());
    }
}
