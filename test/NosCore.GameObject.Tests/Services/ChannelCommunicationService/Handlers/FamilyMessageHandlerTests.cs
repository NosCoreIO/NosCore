//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class FamilyMessageHandlerTests
    {
        private TestWorld World;
        private ClientSession _member;
        private ClientSession _outsider;
        private Mock<ISessionRegistry> _sessionRegistry;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _member = await World.GenerateSessionAsync();
            _outsider = await World.GenerateSessionAsync();
            _member.Character.Family = new NosCore.GameObject.Services.FamilyService.Family { FamilyId = 42, Name = "Testers" };
            _outsider.Character.Family = null;
            var sessions = new[] { _member, _outsider };
            _sessionRegistry = new Mock<ISessionRegistry>();
            _sessionRegistry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>()))
                .Returns((Func<ClientSession, bool>? predicate) => predicate == null ? sessions : sessions.Where(predicate));
            _member.LastPackets.Clear();
            _outsider.LastPackets.Clear();
        }

        private Task ChatAsync(Guid senderMapInstanceId) =>
            new FamilyChatDataMessageChannelCommunicationMessageHandler(_sessionRegistry.Object).Handle(new FamilyChatData
            {
                FamilyId = 42,
                SenderId = 7,
                SenderName = "Alice",
                SenderMapInstanceId = senderMapInstanceId,
                Message = "hello team"
            });

        [TestMethod]
        public async Task FamilyChatOnTheSameMapLetsTheClientNameTheSender()
        {
            await ChatAsync(_member.Character.MapInstanceId);

            var say = _member.LastPackets.OfType<SayPacket>().Single();
            Assert.AreEqual(SayColorType.Blue, say.Type);
            Assert.AreEqual(7, say.VisualId);
            Assert.AreEqual("hello team", say.Message);
            var speak = _member.LastPackets.OfType<SpeakPacket>().Single();
            Assert.AreEqual(SpeakType.Family, speak.SpeakType);
            Assert.AreEqual("Alice", speak.EntityName);
        }

        [TestMethod]
        public async Task FamilyChatFromAnotherMapCarriesTheSenderName()
        {
            await ChatAsync(Guid.NewGuid());

            Assert.AreEqual("[Alice]:hello team", _member.LastPackets.OfType<SayPacket>().Single().Message);
        }

        [TestMethod]
        public async Task FamilyChatStaysInsideTheFamily()
        {
            await ChatAsync(_member.Character.MapInstanceId);

            Assert.AreEqual(0, _outsider.LastPackets.Count);
        }

        [TestMethod]
        public async Task FamilyShoutReachesOnlyTheFamilyInItsLanguage()
        {
            await new FamilyShoutDataMessageChannelCommunicationMessageHandler(_sessionRegistry.Object, World.GameLanguageLocalizer)
                .Handle(new FamilyShoutData { FamilyId = 42, Message = "gather at the farm" });

#pragma warning disable CS0618
            var shout = _member.LastPackets.OfType<MsgPacket>().Single();
#pragma warning restore CS0618
            var expected = string.Format(World.GameLanguageLocalizer[LanguageKey.FAMILY_SHOUT, _member.Account.Language], "gather at the farm");
            Assert.AreEqual(expected, shout.Message);
            Assert.AreEqual(0, _outsider.LastPackets.Count);
        }
    }
}
