//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GuriPacketHandlerTests
    {
        private GuriPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IMessageBus> MessageBus = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            MessageBus = new Mock<IMessageBus>();
            Handler = new GuriPacketHandler(MessageBus.Object);
        }

        [TestMethod]
        public void HandlerCanBeConstructed()
        {
            new Spec("Handler can be constructed")
                .Then(HandlerShouldNotBeNull)
                .Execute();
        }

        [TestMethod]
        public async Task GuriPacketShouldPublishEvent()
        {
            await new Spec("Guri packet should publish GuriPacketReceivedEvent")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacket)
                .Then(MessageBusShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GuriPacketWithTypeShouldPassCorrectData()
        {
            await new Spec("Guri packet with type should publish event carrying that type")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacketWithType)
                .Then(MessageBusShouldBeCalledWithCorrectType)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
        }

        private async Task ExecutingGuriPacket()
        {
            await Handler.ExecuteAsync(new GuriPacket(), Session);
        }

        private async Task ExecutingGuriPacketWithType()
        {
            await Handler.ExecuteAsync(new GuriPacket { Type = GuriPacketType.Title }, Session);
        }

        private void HandlerShouldNotBeNull()
        {
            Assert.IsNotNull(Handler);
        }

        private void MessageBusShouldBeCalled()
        {
            MessageBus.Verify(x => x.PublishAsync(
                It.IsAny<GuriPacketReceivedEvent>(),
                It.IsAny<DeliveryOptions?>()), Times.Once);
        }

        private void MessageBusShouldBeCalledWithCorrectType()
        {
            MessageBus.Verify(x => x.PublishAsync(
                It.Is<GuriPacketReceivedEvent>(e => e.Packet.Type == GuriPacketType.Title),
                It.IsAny<DeliveryOptions?>()), Times.Once);
        }
    }
}
