//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class Mve2PacketHandlerTests : SpecBase
    {
        private const short Kangaroo = 9276;

        private Mve2PacketHandler Handler;

        [TestInitialize]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
            World.WorldConfiguration.Value.BackpackSize = 48;
            Handler = World.Create<Mve2PacketHandler>();
        }

        [TestMethod]
        public async Task AMountMovedToTheMainPocketEmptiesItsSlotThenFillsTheOther()
        {
            await new Spec("A mount moved to the main pocket empties its slot, then fills the other")
                .Given(AMountInSlot_OfTheMountPocket, 2)
                .WhenAsync(Moving_, new Mve2Packet { InventoryType = PocketType.Mount, Slot = 2, DestinationInventoryType = PocketType.Main, DestinationSlot = 5 })
                .Then(TheMountShouldBeInSlot_OfThe_Pocket, 5, NoscorePocketType.Main)
                .And(TheSlotsShouldBeSentAs_, "Mount 2 empty, Main 5 mount")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedMoveSendsBothSlotsAsTheyAre()
        {
            await new Spec("A refused move sends both slots as they are")
                .Given(AMountInSlot_OfTheMountPocket, 2)
                .WhenAsync(Moving_, new Mve2Packet { InventoryType = PocketType.Mount, Slot = 2, DestinationInventoryType = PocketType.Etc, DestinationSlot = 5 })
                .Then(TheMountShouldBeInSlot_OfThe_Pocket, 2, NoscorePocketType.Mount)
                .And(TheSlotsShouldBeSentAs_, "Mount 2 mount, Etc 5 empty")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NothingMovesIntoWhatIsWorn()
        {
            await new Spec("Nothing is moved into what is worn")
                .Given(AMountInSlot_OfTheMountPocket, 2)
                .WhenAsync(Moving_, new Mve2Packet { InventoryType = PocketType.Mount, Slot = 2, DestinationInventoryType = PocketType.Wear, DestinationSlot = 0 })
                .Then(TheMountShouldBeInSlot_OfThe_Pocket, 2, NoscorePocketType.Mount)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void AMountInSlot_OfTheMountPocket(int slot)
        {
            World.ItemList.Add(new Item { Type = NoscorePocketType.Mount, ItemType = ItemType.Special, VNum = Kangaroo });
            World.Inject<IItemCatalog>(new ItemCatalog(World.ItemList));
            var mount = World.GenerateItemProvider().Create(Kangaroo, 1);
            Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(mount, Session.Character.CharacterId), NoscorePocketType.Mount, (short)slot);
        }

        private async Task Moving_(Mve2Packet packet)
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(packet, Session);
        }

        private void TheMountShouldBeInSlot_OfThe_Pocket(int slot, NoscorePocketType pocket) =>
            Assert.AreEqual(Kangaroo, Session.Character.InventoryService.LoadBySlotAndType((short)slot, pocket)?.ItemInstance.ItemVNum);

        private void TheSlotsShouldBeSentAs_(string expected) =>
            Assert.AreEqual(expected, string.Join(", ", Session.LastPackets.OfType<IvnPacket>().Select(p =>
            {
                var slot = (p.IvnSubPackets ?? []).Single();
                return $"{p.Type} {slot.Slot} {(slot.VNum == Kangaroo ? "mount" : slot.VNum is 0 or -1 ? "empty" : slot.VNum.ToString())}";
            })));
    }
}
