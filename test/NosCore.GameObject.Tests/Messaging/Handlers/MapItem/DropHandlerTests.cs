//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
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
        private const short PickedItem = 1012;

        private ClientSession _session = null!;
        private Mock<IMessageBus> _messageBus = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _messageBus = new Mock<IMessageBus>();
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

        private Task PickingUp_Of_(int amount, short vNum) =>
            new DropHandler(_messageBus.Object).Handle(new MapItemPickedUpEvent(
                _session, 100001, vNum, (short)amount,
                TestHelpers.Instance.GenerateItemProvider().Create(vNum, (short)amount),
                new GetPacket { PickerType = VisualType.Player, PickerId = _session.Character.CharacterId, VisualId = 100001 }));

        private void TheCharacterShouldCarry_Of_(int amount, short vNum) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vNum));

        private void TheReceivedItemsShouldBePublished_Of_(int amount, short vNum) =>
            _messageBus.Verify(b => b.PublishAsync(
                It.Is<ItemsReceivedEvent>(e => e.VNum == vNum && e.Amount == amount),
                It.IsAny<DeliveryOptions?>()), Times.Once);
    }
}
