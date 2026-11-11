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
using System;
using System.Threading;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GuriPacketHandlerTests
    {
        private TestWorld World;
        private GuriPacketHandler Handler;
        private ClientSession Session;
        private Mock<IMessageBus> MessageBus;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();

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
        public async Task GuriPacketShouldBeHandledInline()
        {
            await new Spec("Guri packet should be handled inline as GuriPacketReceivedEvent")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacket)
                .Then(MessageBusShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GuriPacketWithTypeShouldPassCorrectData()
        {
            await new Spec("Guri packet with type should be handled as an event carrying that type")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacketWithType)
                .Then(MessageBusShouldBeCalledWithCorrectType)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(0);
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
            MessageBus.Verify(x => x.InvokeAsync(
                It.IsAny<GuriPacketReceivedEvent>(),
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        private void MessageBusShouldBeCalledWithCorrectType()
        {
            MessageBus.Verify(x => x.InvokeAsync(
                It.Is<GuriPacketReceivedEvent>(e => e.Packet.Type == GuriPacketType.Title),
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan?>()), Times.Once);
        }
    }
}
