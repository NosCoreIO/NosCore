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
using NodaTime;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking.SessionGroup;
using NosCore.PacketHandlers.Chat;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Chat
{
    [TestClass]
    public class ClientBubblePacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private readonly List<IPacket> _broadcast = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var map = World.GetBaseMap(1);
            _session.Character.MapInstance = map;
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(packets => _broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            map.Sessions = mapSessions.Object;
        }

        [TestMethod]
        public async Task ABubbleIsShownToTheMapWhileItLasts()
        {
            await new Spec("An announced bubble is shown to the map with the text the server accepted")
                .Given(TheCharacterHasTheBubble_For_Minutes, "Hello there", 30)
                .WhenAsync(TheClientAnnounces_, "something else")
                .Then(TheMapShouldSee_, "Hello there")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExpiredBubbleIsNoLongerShown()
        {
            await new Spec("Once its time is up the bubble is no longer relayed and fades")
                .Given(TheCharacterHasTheBubble_For_Minutes, "Hello there", 30)
                .And(_MinutesPass, 30)
                .WhenAsync(TheClientAnnounces_, "Hello there")
                .Then(TheMapShouldSeeNoBubble)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutABubbleNothingIsShown()
        {
            await new Spec("A character who used no bubble cannot put one up")
                .WhenAsync(TheClientAnnounces_, "Hello there")
                .Then(TheMapShouldSeeNoBubble)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABubbleCannotBeAnnouncedForSomeoneElse()
        {
            await new Spec("A bubble announced for another character is ignored")
                .Given(TheCharacterHasTheBubble_For_Minutes, "Hello there", 30)
                .WhenAsync(TheClientAnnouncesFor_, _session.Character.CharacterId + 1)
                .Then(TheMapShouldSeeNoBubble)
                .ExecuteAsync();
        }

        private void TheCharacterHasTheBubble_For_Minutes(string text, int minutes)
        {
            _session.Character.SpeechBubble = text;
            _session.Character.SpeechBubbleExpiresAt = World.Clock.GetCurrentInstant() + Duration.FromMinutes(minutes);
        }

        private void _MinutesPass(int minutes) => World.Clock.Advance(Duration.FromMinutes(minutes));

        private Task TheClientAnnounces_(string text) =>
            World.Create<ClientBubblePacketHandler>().ExecuteAsync(new ClientBubblePacket
            {
                CharacterId = _session.Character.CharacterId,
                Message = text,
            }, _session);

        private Task TheClientAnnouncesFor_(long characterId) =>
            World.Create<ClientBubblePacketHandler>().ExecuteAsync(new ClientBubblePacket
            {
                CharacterId = characterId,
                Message = "Hello there",
            }, _session);

        private void TheMapShouldSee_(string text)
        {
            var bubble = _broadcast.OfType<BubblePacket>().Single();
            Assert.AreEqual(_session.Character.CharacterId, bubble.CharacterId);
            Assert.AreEqual(text, bubble.Message);
        }

        private void TheMapShouldSeeNoBubble() => Assert.IsFalse(_broadcast.OfType<BubblePacket>().Any());
    }
}
