//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Drops;
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

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _messageBus = World.Freeze<Mock<IMessageBus>>();
        }

        [TestMethod]
        public async Task PickingUpAnItemAnnouncesIt()
        {
            await new Spec("A drop that lands in the inventory publishes the item and amount received")
                .WhenAsync(PickingUp_Of_, 3, PickedItem)
                .Then(TheCharacterShouldCarry_Of_, 3, PickedItem)
                .And(TheReceivedItemsShouldBePublished_Of_, 3, PickedItem)
                .ExecuteAsync();
        }

        private Task PickingUp_Of_(int amount, short vNum)
        {
            var item = _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(vNum, (short)amount), _session.Character.CharacterId)).NotNull().First().ItemInstance;
            var mapItem = _session.Character.MapInstance.PutItem((short)amount, item, _session).NotNull();
            return World.Create<DropHandler>().Handle(new MapItemPickedUpEvent(
                _session, mapItem.VisualId, vNum, (short)amount, mapItem.ItemInstance,
                new GetPacket { PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = mapItem.VisualId }));
        }

        private void TheCharacterShouldCarry_Of_(int amount, short vNum) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vNum));

        private void TheReceivedItemsShouldBePublished_Of_(int amount, short vNum) =>
            _messageBus.Verify(b => b.PublishAsync(
                It.Is<ItemsReceivedEvent>(e => e.VNum == vNum && e.Amount == amount),
                It.IsAny<DeliveryOptions?>()), Times.Once);
    }
}
