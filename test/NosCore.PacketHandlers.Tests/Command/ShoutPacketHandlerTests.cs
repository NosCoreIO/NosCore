//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class ShoutPacketHandlerTests
    {
        private ShoutPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<ISerializer> Serializer = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            Serializer = new Mock<ISerializer>();

            Serializer.Setup(x => x.Serialize(It.IsAny<IPacket[]>()))
                .Returns("serialized_packet");

            Handler = new ShoutPacketHandler(
                Serializer.Object,
                PubSubHub.Object,
                TestHelpers.Instance.GameLanguageLocalizer);
        }

        [TestMethod]
        public async Task ShoutingShouldBroadcastToAllPlayers()
        {
            await new Spec("Shouting should broadcast to all players")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoutingMessage)
                .Then(MessagesShouldBeBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoutingShouldSerializeBothPacketTypes()
        {
            await new Spec("Shouting should serialize both say and msg packets")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoutingMessage)
                .Then(SerializerShouldBeCalledTwice)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task ShoutingMessage()
        {
            await Handler.ExecuteAsync(new ShoutPacket
            {
                Message = "Test announcement message"
            }, Session);
        }

        private void MessagesShouldBeBroadcast()
        {
            PubSubHub.Verify(x => x.SendMessagesAsync(It.IsAny<List<IMessage>>()), Times.Once);
        }

        private void SerializerShouldBeCalledTwice()
        {
            Serializer.Verify(x => x.Serialize(It.IsAny<IPacket[]>()), Times.Exactly(2));
        }
    }
}
