//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FriendInvitePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private ClientSession TargetSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            TargetSession = await World.CreateSessionAsync();
            World.FriendHub.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .ReturnsAsync(LanguageKey.FRIEND_REQUEST_SENT);
        }

        [TestMethod]
        public async Task TypingAConnectedNameAsksThatPlayer()
        {
            await new Spec("Typing a connected player's name asks them to become a friend")
                .WhenAsync(Adding_, (string?)null)
                .Then(TheTargetShouldBeAskedToBecomeAFriend)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownNameIsAnswered()
        {
            await new Spec("A name nobody connected has is answered with the client's own text")
                .WhenAsync(Adding_, "Nobody")
                .Then(TheSenderShouldBeToldTheCharacterIsUnknown)
                .And(NoRequestShouldBeSent)
                .ExecuteAsync();
        }

        private Task Adding_(string? name) =>
            World.Create<FriendInvitePacketHandler>().ExecuteAsync(
                new FriendInvitePacket { CharacterName = name ?? TargetSession.Character.Name }, Session);

        private void TheTargetShouldBeAskedToBecomeAFriend()
        {
            var question = TargetSession.LastPackets.OfType<Dlgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.AskBecomeFriend, question.Question);
            var senderId = Session.Character.CharacterId;
            var targetId = TargetSession.Character.VisualId;
            World.FriendHub.Verify(s => s.AddFriendAsync(It.Is<FriendShipRequest>(r =>
                r.CharacterId == senderId && r.FinsPacket.CharacterId == targetId)), Times.Once);
        }

        private void TheSenderShouldBeToldTheCharacterIsUnknown() =>
            Assert.AreEqual(Game18NConstString.UnknownCharacter, Session.LastPackets.OfType<InfoiPacket>().Last().Message);

        private void NoRequestShouldBeSent() =>
            World.FriendHub.Verify(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()), Times.Never);
    }
}
