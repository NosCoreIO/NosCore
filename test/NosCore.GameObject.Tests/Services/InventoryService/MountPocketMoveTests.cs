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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class MountPocketMoveTests
    {
        private const short Kangaroo = 9276;
        private const short Seed = 1012;

        private TestWorld World;
        private ClientSession Session;
        private IInventoryService Inventory => Session.Character.InventoryService;
        private InventoryItemInstance? Moved;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.WorldConfiguration.Value.BackpackSize = 48;
            World.ItemList.Add(new Item { Type = NoscorePocketType.Mount, ItemType = ItemType.Special, VNum = Kangaroo });
            World.Inject<IItemCatalog>(new ItemCatalog(World.ItemList));
            Session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public void AMountMovesFromItsPocketToTheMainPocket()
        {
            new Spec("A mount moves from its pocket to a free slot of the main pocket")
                .Given(_IsInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 2, NoscorePocketType.Mount, 5, NoscorePocketType.Main)
                .Then(_ShouldBeInSlot_OfThe_Pocket, Kangaroo, 5, NoscorePocketType.Main)
                .And(Slot_OfThe_PocketShouldBeEmpty, 2, NoscorePocketType.Mount)
                .Execute();
        }

        [TestMethod]
        public void AMountMovesBackToItsPocket()
        {
            new Spec("A mount moves back from the main pocket to its own")
                .Given(_IsInSlot_OfThe_Pocket, Kangaroo, 5, NoscorePocketType.Main)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 5, NoscorePocketType.Main, 0, NoscorePocketType.Mount)
                .Then(_ShouldBeInSlot_OfThe_Pocket, Kangaroo, 0, NoscorePocketType.Mount)
                .Execute();
        }

        [TestMethod]
        public void AMountDoesNotGoToTheEtcPocket()
        {
            new Spec("A mount does not go to the etc pocket")
                .Given(_IsInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 2, NoscorePocketType.Mount, 5, NoscorePocketType.Etc)
                .Then(NothingShouldHaveMoved)
                .And(_ShouldBeInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .Execute();
        }

        [TestMethod]
        public void AnotherItemDoesNotGoToTheMountPocket()
        {
            new Spec("An item that is not a mount does not go to the mount pocket")
                .Given(_IsInSlot_OfThe_Pocket, Seed, 3, NoscorePocketType.Main)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 3, NoscorePocketType.Main, 0, NoscorePocketType.Mount)
                .Then(NothingShouldHaveMoved)
                .And(_ShouldBeInSlot_OfThe_Pocket, Seed, 3, NoscorePocketType.Main)
                .Execute();
        }

        [TestMethod]
        public void AMountDoesNotReplaceWhatHoldsTheSlot()
        {
            new Spec("A mount does not replace what holds the slot it is dropped on")
                .Given(_IsInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .And(_IsInSlot_OfThe_Pocket, Seed, 5, NoscorePocketType.Main)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 2, NoscorePocketType.Mount, 5, NoscorePocketType.Main)
                .Then(NothingShouldHaveMoved)
                .And(_ShouldBeInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .And(_ShouldBeInSlot_OfThe_Pocket, Seed, 5, NoscorePocketType.Main)
                .Execute();
        }

        [TestMethod]
        public void ASlotPastTheEndOfThePocketIsRefused()
        {
            new Spec("A slot past the end of the pocket is refused")
                .Given(_IsInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .When(MovingSlot_OfThe_PocketToSlot_OfThe_Pocket, 2, NoscorePocketType.Mount, 300, NoscorePocketType.Main)
                .Then(NothingShouldHaveMoved)
                .And(_ShouldBeInSlot_OfThe_Pocket, Kangaroo, 2, NoscorePocketType.Mount)
                .Execute();
        }

        private void _IsInSlot_OfThe_Pocket(short vnum, int slot, NoscorePocketType pocket) =>
            Inventory.AddItemToPocket(InventoryItemInstance.Create(World.GenerateItemProvider().Create(vnum, 1),
                Session.Character.CharacterId), pocket, (short)slot);

        private void MovingSlot_OfThe_PocketToSlot_OfThe_Pocket(int slot, NoscorePocketType pocket, int destination,
            NoscorePocketType destinationPocket) =>
            Moved = Inventory.MoveInPocket((short)slot, pocket, destinationPocket, (short)destination, false);

        private void _ShouldBeInSlot_OfThe_Pocket(short vnum, int slot, NoscorePocketType pocket)
        {
            Assert.AreEqual(vnum, Inventory.LoadBySlotAndType((short)slot, pocket)?.ItemInstance.ItemVNum);
            Assert.AreEqual(1, Inventory.Values.Count(i => i.ItemInstance.ItemVNum == vnum), "the item is held once");
        }

        private void Slot_OfThe_PocketShouldBeEmpty(int slot, NoscorePocketType pocket) =>
            Assert.IsNull(Inventory.LoadBySlotAndType((short)slot, pocket));

        private void NothingShouldHaveMoved() => Assert.IsNull(Moved);
    }
}
