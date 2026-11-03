//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class FamilyChangedDataMessageHandlerTests
    {
        private ClientSession _member = null!;
        private ClientSession _joiner = null!;
        private ClientSession _bystander = null!;
        private Mock<IFamilyService> _familyService = null!;
        private FamilyChangedDataMessageChannelCommunicationMessageHandler _handler = null!;
        private readonly NosCore.GameObject.Services.FamilyService.Family _family = new() { FamilyId = 42, Name = "Testers", MaxSize = 50 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _member = await TestHelpers.Instance.GenerateSessionAsync();
            _joiner = await TestHelpers.Instance.GenerateSessionAsync();
            _bystander = await TestHelpers.Instance.GenerateSessionAsync();
            _member.Character.Family = _family;
            _joiner.Character.Family = null;
            _bystander.Character.Family = null;
            var sessions = new[] { _member, _joiner, _bystander };
            var registry = new Mock<ISessionRegistry>();
            registry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>()))
                .Returns((Func<ClientSession, bool>? predicate) => predicate == null ? sessions : sessions.Where(predicate));
            _familyService = new Mock<IFamilyService>();
            _familyService.Setup(s => s.GetFamilyAsync(It.IsAny<long>())).ReturnsAsync(_family);
            var bystanderId = _bystander.Character.CharacterId;
            _familyService.Setup(s => s.GetFamilyAsync(bystanderId)).ReturnsAsync((NosCore.GameObject.Services.FamilyService.Family?)null);
            _handler = new FamilyChangedDataMessageChannelCommunicationMessageHandler(registry.Object, _familyService.Object,
                TestHelpers.Instance.GameLanguageLocalizer, new FamilyExperienceService());
            foreach (var session in sessions)
            {
                session.LastPackets.Clear();
            }
        }

        [TestMethod]
        public async Task TheNewMemberGetsTheFamilyAndTheMapSeesTheTag()
        {
            await _handler.Handle(new FamilyChangedData { FamilyId = 42, CharacterIds = { _joiner.Character.CharacterId }, JoinedCharacterName = "Bob" });

            Assert.AreEqual(42, _joiner.Character.Family?.FamilyId);
            Assert.IsTrue(_joiner.LastPackets.OfType<GInfoPacket>().Any());
            var tag = _bystander.LastPackets.OfType<GidxPacket>().Single();
            Assert.AreEqual(_joiner.Character.VisualId, tag.VisualId);
            Assert.AreEqual(42, tag.FamilyId);
        }

        [TestMethod]
        public async Task EveryMemberHearsWhoJoined()
        {
            await _handler.Handle(new FamilyChangedData { FamilyId = 42, CharacterIds = { _joiner.Character.CharacterId }, JoinedCharacterName = "Bob" });

            foreach (var session in new[] { _member, _joiner })
            {
                var joined = session.LastPackets.OfType<Msgi2Packet>().Single();
                Assert.AreEqual(Game18NConstString.JoinedFamily, joined.Message);
            }

            Assert.AreEqual(0, _bystander.LastPackets.OfType<Msgi2Packet>().Count());
        }

        [TestMethod]
        public async Task SomeoneWhoLeftLosesTheFamily()
        {
            _bystander.Character.Family = _family;

            await _handler.Handle(new FamilyChangedData { FamilyId = 42, CharacterIds = { _bystander.Character.CharacterId } });

            Assert.IsNull(_bystander.Character.Family);
            var tag = _member.LastPackets.OfType<GidxPacket>().Single();
            Assert.AreEqual(_bystander.Character.VisualId, tag.VisualId);
            Assert.IsNull(tag.FamilyId);
        }
    }
}
