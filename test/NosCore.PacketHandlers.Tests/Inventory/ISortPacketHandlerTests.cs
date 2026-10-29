//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class ISortPacketHandlerTests : SpecBase
    {
        private ISortPacketHandler Handler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Handler = new ISortPacketHandler();
        }

        [TestMethod]
        public async Task SortingShouldPackThePocketAndRefreshEveryTouchedSlot()
        {
            await new Spec("Sorting should pack the pocket and refresh every touched slot")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Sorting_, PocketType.Main)
                .Then(ItemShouldBeInSlot_, 0)
                .And(OneIvnShouldListSlots_, "0 1")
                .And(Slot1ShouldBeSentEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SortingThePocketOfWornItemsShouldDoNothing()
        {
            await new Spec("Sorting the pocket of worn items should do nothing")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .WhenAsync(Sorting_, (PocketType)NoscorePocketType.Wear)
                .Then(ItemShouldBeInSlot_, 1)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SortingWhileInAShopShouldDoNothing()
        {
            await new Spec("Sorting while in a shop should do nothing")
                .Given(MainPocketHasAnItemInItsLastSlot)
                .And(CharacterIsInShop)
                .WhenAsync(Sorting_, PocketType.Main)
                .Then(ItemShouldBeInSlot_, 1)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SortingAnEmptyPocketShouldSendNothing()
        {
            await new Spec("Sorting an empty pocket should send nothing")
                .WhenAsync(Sorting_, PocketType.Etc)
                .Then(NoPacketShouldBeSent)
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

        private void ItemShouldBeInSlot_(int slot) =>
            Assert.AreEqual((short)1012,
                Session.Character.InventoryService.LoadBySlotAndType((short)slot, NoscorePocketType.Main)?.ItemInstance.ItemVNum);

        private void OneIvnShouldListSlots_(string slots)
        {
            var ivn = Session.LastPackets.OfType<IvnPacket>().Single();
            Assert.AreEqual(PocketType.Main, ivn.Type);
            Assert.AreEqual(slots, string.Join(" ", ivn.IvnSubPackets!.Select(s => s!.Slot)));
        }

        private void Slot1ShouldBeSentEmpty()
        {
            var ivn = Session.LastPackets.OfType<IvnPacket>().Single();
            var empty = ((IItemInstance?)null).GenerateIvnSubPacket(PocketType.Main, 1);
            Assert.AreEqual(empty.VNum, ivn.IvnSubPackets!.Single(s => s!.Slot == 1)!.VNum);
        }
    }
}
