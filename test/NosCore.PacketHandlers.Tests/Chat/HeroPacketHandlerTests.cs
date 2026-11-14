//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ReputationRankingService;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS0618

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class HeroPacketHandlerTests
    {
        private TestWorld World;
        private HeroPacketHandler Handler;
        private ClientSession Session;
        private Mock<IPubSubHub> PubSubHub;
        private Mock<IReputationRankingService> Ranking;
        private readonly List<IPacket> Serialized = new();
        private readonly List<PostedPacket> Posted = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            PubSubHub = new Mock<IPubSubHub>();
            Ranking = new Mock<IReputationRankingService>();
            var serializer = new Mock<ISerializer>();

            serializer.Setup(x => x.Serialize(It.IsAny<IEnumerable<IPacket>>()))
                .Callback<IEnumerable<IPacket>>(packets => Serialized.AddRange(packets))
                .Returns("serialized");
            PubSubHub.Setup(x => x.SendMessageAsync(It.IsAny<IMessage>()))
                .Callback<IMessage>(message => Posted.Add((PostedPacket)message))
                .ReturnsAsync(true);

            Handler = new HeroPacketHandler(serializer.Object, PubSubHub.Object, Ranking.Object);
        }

        [TestMethod]
        public async Task TopThreePlayerMessageIsBroadcastToEveryChannel()
        {
            await new Spec("Top three player message is broadcast to every channel")
                .Given(SenderIsTopThree)
                .WhenAsync(SendingAHeroMessage)
                .Then(HeroMessageShouldBePostedToPlayersAllowingHeroChat)
                .And(SenderShouldNotBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayerOutsideTopThreeIsRefused()
        {
            await new Spec("Player outside top three is refused")
                .Given(SenderIsNotTopThree)
                .WhenAsync(SendingAHeroMessage)
                .Then(SenderShouldBeRefused)
                .And(NothingShouldBePosted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmptyMessageIsIgnored()
        {
            await new Spec("Empty message is ignored")
                .Given(SenderIsTopThree)
                .WhenAsync(SendingAnEmptyHeroMessage)
                .Then(NothingShouldBePosted)
                .And(SenderShouldNotBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlankMessageIsIgnored()
        {
            await new Spec("Blank message is ignored")
                .Given(SenderIsTopThree)
                .WhenAsync(SendingABlankHeroMessage)
                .Then(NothingShouldBePosted)
                .And(SenderShouldNotBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RankingIsAskedForTheSenderLiveState()
        {
            await new Spec("Ranking is asked for the sender live state")
                .Given(SenderIsTopThree)
                .And(SenderHasReputation_, 12345L)
                .WhenAsync(SendingAHeroMessage)
                .Then(RankingShouldBeAskedWithReputation_, 12345L)
                .ExecuteAsync();
        }

        private void SenderIsTopThree() => SetRanking(true);

        private void SenderIsNotTopThree() => SetRanking(false);

        private void SenderHasReputation_(long reputation) => Session.Character.Reputation = reputation;

        private void SetRanking(bool isTopThree) =>
            Ranking.Setup(x => x.IsTopThree(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<AuthorityType>()))
                .Returns(isTopThree);

        private Task SendingAHeroMessage() =>
            Handler.ExecuteAsync(new HeroPacket { Message = "  Hello heroes  " }, Session);

        private Task SendingAnEmptyHeroMessage() =>
            Handler.ExecuteAsync(new HeroPacket { Message = "" }, Session);

        private Task SendingABlankHeroMessage() =>
            Handler.ExecuteAsync(new HeroPacket { Message = "   " }, Session);

        private void HeroMessageShouldBePostedToPlayersAllowingHeroChat()
        {
            Assert.AreEqual(1, Posted.Count);
            Assert.AreEqual(ReceiverType.AllNoHeroBlocked, Posted[0].ReceiverType);
            Assert.AreEqual(Session.Character.CharacterId, Posted[0].SenderCharacter.NotNull().Id);
            var msg = Serialized.OfType<MsgPacket>().Single();
            Assert.AreEqual(MessageType.Hero, msg.Type);
            Assert.AreEqual($"[{Session.Character.Name}]:Hello heroes", msg.Message);
        }

        private void RankingShouldBeAskedWithReputation_(long reputation)
        {
            var characterId = Session.Character.CharacterId;
            var authority = Session.Account.Authority;
            Ranking.Verify(x => x.IsTopThree(characterId, reputation, authority), Times.Once);
        }

        private void SenderShouldBeRefused()
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.OnlyTheTopThreePlayers, packet.Message);
            Assert.AreEqual(SayColorType.Red, packet.Type);
        }

        private void SenderShouldNotBeRefused() =>
            Assert.IsFalse(Session.LastPackets.OfType<SayiPacket>().Any());

        private void NothingShouldBePosted() =>
            PubSubHub.Verify(x => x.SendMessageAsync(It.IsAny<IMessage>()), Times.Never);
    }
}
