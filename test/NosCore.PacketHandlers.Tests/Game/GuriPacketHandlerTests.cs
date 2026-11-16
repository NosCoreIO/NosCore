//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
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

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GuriPacketHandlerTests
    {
        private TestWorld World;
        private GuriPacketHandler Handler;
        private ClientSession Session;
        private Mock<IGameEventDispatcher> GameEvents;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();

            GameEvents = World.Freeze<Mock<IGameEventDispatcher>>();
            Handler = World.Create<GuriPacketHandler>();
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
            GameEvents.Verify(x => x.RaiseAsync(
                It.IsAny<GuriPacketReceivedEvent>()), Times.Once);
        }

        private void MessageBusShouldBeCalledWithCorrectType()
        {
            GameEvents.Verify(x => x.RaiseAsync(
                It.Is<GuriPacketReceivedEvent>(e => e.Packet.Type == GuriPacketType.Title)), Times.Once);
        }
    }
}
