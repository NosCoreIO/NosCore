//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using Wolverine;

namespace NosCore.GameObject.Tests.Messaging.Handlers.MapItem
{
    [TestClass]
    public class DropHandlerTests
    {
        private TestWorld World;
        private const short PickedItem = 1012;

        private ClientSession _session;
        private Mock<IMessageBus> _messageBus;
        private MapItemComponentBundle _drop;
        private short _droppedVNum;
        private short _droppedAmount;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _messageBus = World.Freeze<Mock<IMessageBus>>();
        }

        [TestMethod]
        public async Task PickingUpAnItemAnnouncesIt()
        {
            await new Spec("A drop that lands in the inventory publishes the item and amount received")
                .Given(ADropOf_Of_, 3, PickedItem)
                .WhenAsync(PickingItUp)
                .Then(TheCharacterShouldCarry_Of_, 3, PickedItem)
                .And(TheReceivedItemsShouldBePublished_Of_, 3, PickedItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APickUpThatOverflowsAStackShowsBothSlots()
        {
            await new Spec("A pick-up too big for the stack held fills it and shows the rest in a new slot")
                .Given(ADropOf_Of_, 5, PickedItem)
                .And(TheCharacterHolds_Of_, 997, PickedItem)
                .WhenAsync(PickingItUp)
                .Then(TheCharacterShouldCarry_Of_, 1002, PickedItem)
                .And(TheClientShouldBeShownSlot_With_, 0, 999)
                .And(TheClientShouldBeShownSlot_With_, 1, 3)
                .ExecuteAsync();
        }

        private void TheCharacterHolds_Of_(int amount, short vNum) =>
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(vNum, (short)amount), _session.Character.CharacterId));

        private void TheClientShouldBeShownSlot_With_(int slot, int amount)
        {
            var shown = _session.LastPackets.OfType<IvnPacket>().SelectMany(p => p.IvnSubPackets ?? []).OfType<IvnSubPacket>()
                .Last(i => i.Slot == slot);
            Assert.AreEqual(amount, shown.RareAmount);
        }

        private void ADropOf_Of_(int amount, short vNum)
        {
            var item = _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(vNum, (short)amount), _session.Character.CharacterId)).NotNull().First().ItemInstance;
            _drop = _session.Character.MapInstance.PutItem((short)amount, item, _session).NotNull();
            _droppedVNum = vNum;
            _droppedAmount = (short)amount;
        }

        private Task PickingItUp() =>
            World.Create<DropHandler>().Handle(new MapItemPickedUpEvent(
                _session, _drop.VisualId, _droppedVNum, _droppedAmount, _drop.ItemInstance,
                new GetPacket { PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = _drop.VisualId }));

        private void TheCharacterShouldCarry_Of_(int amount, short vNum) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vNum));

        private void TheReceivedItemsShouldBePublished_Of_(int amount, short vNum) =>
            _messageBus.Verify(b => b.PublishAsync(
                It.Is<ItemsReceivedEvent>(e => e.VNum == vNum && e.Amount == amount),
                It.IsAny<DeliveryOptions?>()), Times.Once);
    }
}
