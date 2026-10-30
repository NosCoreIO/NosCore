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
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class SortOpenPacketHandlerTests : SpecBase
    {
        private SortOpenPacketHandler Handler = null!;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            Handler = new SortOpenPacketHandler();
        }

        [TestMethod]
        public async Task OpeningShouldPackSpecialistsAndResendBothPockets()
        {
            await new Spec("Opening should pack specialists and resend both pockets")
                .Given(SpecialistIsInSlot_, 3)
                .WhenAsync(OpeningTheSortedPockets)
                .Then(SpecialistShouldBeInSlot_, 0)
                .And(InvForSpecialistsShouldList_, "0:912")
                .And(InvForCostumesShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningWhileInAShopShouldDoNothing()
        {
            await new Spec("Opening while in a shop should do nothing")
                .Given(SpecialistIsInSlot_, 3)
                .And(CharacterIsInShop)
                .WhenAsync(OpeningTheSortedPockets)
                .Then(SpecialistShouldBeInSlot_, 3)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void SpecialistIsInSlot_(int slot)
        {
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(912, 1), Session.Character.CharacterId),
                NoscorePocketType.Specialist, (short)slot);
        }

        private async Task OpeningTheSortedPockets()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new SortOpenPacket(), Session);
        }

        private void SpecialistShouldBeInSlot_(int slot) =>
            Assert.AreEqual((short)912,
                Session.Character.InventoryService.LoadBySlotAndType((short)slot, NoscorePocketType.Specialist)?.ItemInstance.ItemVNum);

        private void InvForSpecialistsShouldList_(string expected)
        {
            var inv = Session.LastPackets.OfType<InvPacket>().Single(p => p.Type == PocketType.Specialist);
            Assert.AreEqual(expected, string.Join(" ", inv.IvnSubPackets!.Select(s => $"{s!.Slot}:{s.VNum}")));
        }

        private void InvForCostumesShouldBeEmpty() =>
            Assert.AreEqual(0, Session.LastPackets.OfType<InvPacket>().Single(p => p.Type == PocketType.Costume).IvnSubPackets!.Count);
    }
}
