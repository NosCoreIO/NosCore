//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Relations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class FriendNotificationDisconnectHandlerTests
    {
        private TestWorld World;
        [TestMethod]
        public async Task FriendsAreToldWhoWentOfflineByName()
        {
            World = await TestWorld.CreateAsync();
            var session = await World.GenerateSessionAsync();
            var characterId = session.Character.CharacterId;
            var friendHub = new Mock<IFriendHub>();
            friendHub.Setup(s => s.GetFriendsAsync(characterId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = 2, RelationType = CharacterRelationType.Friend },
            });
            var pubSubHub = new Mock<IPubSubHub>();
            List<IMessage>? sent = null;
            pubSubHub.Setup(s => s.SendMessagesAsync(It.IsAny<List<IMessage>>()))
                .Callback<List<IMessage>>(m => sent = m)
                .ReturnsAsync(true);
            var serializer = new Serializer(new[] { typeof(FinfoPacket), typeof(FinfoSubPackets) });

            await new FriendNotificationDisconnectHandler(friendHub.Object, pubSubHub.Object, serializer)
                .HandleDisconnectAsync(session);

            var posted = (PostedPacket)sent.NotNull().Single();
            Assert.AreEqual($"finfo {characterId}.0.{session.Character.Name}", posted.Packet);
        }
    }
}
