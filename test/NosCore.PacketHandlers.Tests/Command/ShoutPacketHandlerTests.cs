//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<IPacket> Serialized = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            PubSubHub = World.Freeze<Mock<IPubSubHub>>();
            Serializer = World.Freeze<Mock<ISerializer>>();

            Serializer.Setup(x => x.Serialize(It.IsAny<IPacket[]>()))
                .Callback<IEnumerable<IPacket>>(packets => Serialized.AddRange(packets))
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

        [TestMethod]
        public async Task TheShoutGoesOutAsAServerLineCarryingItsText()
        {
            await new Spec("The shout goes out as a server line carrying its text")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoutingMessage)
                .Then(TheSayi2LineShouldCarryTheTextAsItsOnlyArgument)
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

        private void TheSayi2LineShouldCarryTheTextAsItsOnlyArgument()
        {
            PacketAssert.AreEqual(new Sayi2Packet
            {
                VisualType = VisualType.Player,
                VisualId = -1,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.Administrator,
                ArgumentType = 999,
                Game18NArguments = { "Test announcement message" },
            }, Serialized.OfType<Sayi2Packet>().Single());
        }

        private void SerializerShouldBeCalledTwice()
        {
            Serializer.Verify(x => x.Serialize(It.IsAny<IPacket[]>()), Times.Exactly(2));
        }
    }
}
