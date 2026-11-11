//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class EmoticonHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ClientSession _onlooker;
        private EmoticonHandler _handler;
        private ISessionMatcher _broadcastMatcher;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.GenerateSessionAsync();
            _onlooker = await World.GenerateSessionAsync();
            var map = World.GetBaseMap(1);
            _session.Character.MapInstance = map;
            _onlooker.Character.MapInstance = map;
            World.SessionRegistry.Register(new SessionInfo
            {
                ChannelId = _onlooker.Channel.NotNull().Id,
                SessionId = _onlooker.SessionId,
                Sender = _onlooker,
                AccountName = _onlooker.Account.Name,
                Disconnect = () => Task.CompletedTask,
                CharacterId = _onlooker.Character.CharacterId,
                MapInstanceId = map.MapInstanceId,
            });
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((_, matcher) => _broadcastMatcher = matcher)
                .Returns(Task.CompletedTask);
            map.Sessions = mapSessions.Object;
            _handler = World.Create<EmoticonHandler>();
        }

        [TestMethod]
        public async Task DataBelowRangeIsIgnored()
        {
            await new Spec("Data < 973 is out of the emoticon range and ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 972, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DataAboveRangeIsIgnored()
        {
            await new Spec("Data > 999 is out of the emoticon range and ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 1000, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WrongGuriTypeIsIgnored()
        {
            await new Spec("Guri type other than TextInput is ignored by the emoticon handler")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.AfterSumming, 973, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EmoticonsBlockedSuppressesBroadcast()
        {
            await new Spec("Character with EmoticonsBlocked flag does not broadcast the effect")
                .Given(EmoticonsAreBlocked)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, _Owner())
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlookerBlockingEmoticonsDoesNotSeeTheEffect()
        {
            await new Spec("An onlooker who blocks emoticons is left out of the broadcast")
                .Given(EmoticonsAreAllowed)
                .And(OnlookerBlocksEmoticons)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, _Owner())
                .Then(OnlookerShouldNotReceiveTheEffect)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlookerAllowingEmoticonsSeesTheEffect()
        {
            await new Spec("An onlooker who allows emoticons receives the broadcast")
                .Given(EmoticonsAreAllowed)
                .And(OnlookerAllowsEmoticons)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, _Owner())
                .Then(OnlookerShouldReceiveTheEffect)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpoofedVisualIdIsIgnored()
        {
            await new Spec("Packet with a VisualId not matching the caller's character is anti-spoof ignored")
                .Given(EmoticonsAreAllowed)
                .WhenAsync(HandlingGuri_, GuriPacketType.TextInput, 975, 999999L)
                .Then(NoEffPacketShouldHaveBeenBroadcast)
                .ExecuteAsync();
        }

        private long _Owner() => _session.Character.CharacterId;

        private void EmoticonsAreAllowed() => _session.Character.EmoticonsBlocked = false;
        private void EmoticonsAreBlocked() => _session.Character.EmoticonsBlocked = true;
        private void OnlookerBlocksEmoticons() => _onlooker.Character.EmoticonsBlocked = true;
        private void OnlookerAllowsEmoticons() => _onlooker.Character.EmoticonsBlocked = false;

        private void OnlookerShouldNotReceiveTheEffect()
        {
            Assert.IsNotNull(_broadcastMatcher);
            Assert.IsFalse(_broadcastMatcher.Matches(_onlooker.Channel.NotNull().Id));
            Assert.IsTrue(_broadcastMatcher.Matches(_session.Channel.NotNull().Id));
        }

        private void OnlookerShouldReceiveTheEffect()
        {
            Assert.IsNotNull(_broadcastMatcher);
            Assert.IsTrue(_broadcastMatcher.Matches(_onlooker.Channel.NotNull().Id));
        }

        private async Task HandlingGuri_(GuriPacketType type, int data, long visualId)
        {
            var packet = new GuriPacket
            {
                Type = type,
                Data = data,
                VisualId = visualId,
            };
            await _handler.Handle(new GuriPacketReceivedEvent(_session, packet));
        }

        private void NoEffPacketShouldHaveBeenBroadcast()
        {
            Assert.IsNull(_broadcastMatcher, "Expected no broadcast");
            Assert.IsFalse(_session.LastPackets.OfType<EffectPacket>().Any(),
                "Expected no EffectPacket broadcast");
        }
    }
}
