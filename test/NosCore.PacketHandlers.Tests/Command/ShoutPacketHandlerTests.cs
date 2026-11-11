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
        private TestWorld World;
        private ShoutPacketHandler Handler;
        private ClientSession Session;
        private Mock<IPubSubHub> PubSubHub;
        private Mock<ISerializer> Serializer;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            PubSubHub = World.Freeze<Mock<IPubSubHub>>();
            Serializer = World.Freeze<Mock<ISerializer>>();

            Serializer.Setup(x => x.Serialize(It.IsAny<IPacket[]>()))
                .Returns("serialized_packet");

            Handler = World.Create<ShoutPacketHandler>();
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
            Session.Character.MapInstance = World.GetBaseMap(1);
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
