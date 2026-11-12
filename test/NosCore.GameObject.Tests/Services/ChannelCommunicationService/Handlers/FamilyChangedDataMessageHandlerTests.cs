//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class FamilyChangedDataMessageHandlerTests
    {
        private TestWorld World;
        private ClientSession _member;
        private ClientSession _joiner;
        private ClientSession _bystander;
        private Mock<IFamilyService> _familyService;
        private FamilyChangedDataMessageChannelCommunicationMessageHandler _handler;
        private readonly NosCore.GameObject.Services.FamilyService.Family _family = new() { FamilyId = 42, Name = "Testers", MaxSize = 50 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _member = await World.CreateSessionAsync();
            _joiner = await World.CreateSessionAsync();
            _bystander = await World.CreateSessionAsync();
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
            _familyService.Setup(s => s.GetMemberCharactersAsync(It.IsAny<NosCore.GameObject.Services.FamilyService.Family>())).ReturnsAsync(new List<CharacterDto>());
            var pubSubHub = new Mock<IPubSubHub>();
            pubSubHub.Setup(s => s.GetSubscribersAsync()).ReturnsAsync(new List<Subscriber>());
            var management = new FamilyManagementService(_familyService.Object, pubSubHub.Object, new FamilyExperienceService(),
                registry.Object,
                new Mock<IFamilyLogService>().Object);
            _handler = new FamilyChangedDataMessageChannelCommunicationMessageHandler(registry.Object, _familyService.Object,
                World.GameLanguageLocalizer, management);
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
        public async Task EveryMemberGetsTheRefreshedMemberList()
        {
            await _handler.Handle(new FamilyChangedData { FamilyId = 42 });

            Assert.IsTrue(_member.LastPackets.OfType<GInfoPacket>().Any());
            Assert.IsTrue(_member.LastPackets.OfType<GmbrPacket>().Any());
            Assert.IsTrue(_member.LastPackets.OfType<GmsgPacket>().Any());
            Assert.AreEqual(0, _bystander.LastPackets.OfType<GmbrPacket>().Count());
        }

        [TestMethod]
        public async Task EveryMemberHearsTheFamilyLevelledUp()
        {
            await _handler.Handle(new FamilyChangedData { FamilyId = 42, ReachedFamilyLevel = 2 });

            var announced = _member.LastPackets.OfType<MsgiPacket>().Single();
            Assert.AreEqual(Game18NConstString.FamilyLevelIncreased, announced.Message);
            Assert.AreEqual(2L, announced.Game18NArguments[0]);
            Assert.AreEqual(0, _bystander.LastPackets.OfType<MsgiPacket>().Count());
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
        public async Task TheNewMemberIsToldTheHistoryCanBeRead()
        {
            await _handler.Handle(new FamilyChangedData { FamilyId = 42, CharacterIds = { _joiner.Character.CharacterId }, JoinedCharacterName = "Bob" });

            Assert.AreEqual(1, _joiner.LastPackets.OfType<FhisStcPacket>().Count());
            Assert.AreEqual(0, _member.LastPackets.OfType<FhisStcPacket>().Count());
        }

        [TestMethod]
        public async Task SomeoneWhoLeftIsNotToldAboutTheHistory()
        {
            _bystander.Character.Family = _family;

            await _handler.Handle(new FamilyChangedData { FamilyId = 42, CharacterIds = { _bystander.Character.CharacterId } });

            Assert.AreEqual(0, _bystander.LastPackets.OfType<FhisStcPacket>().Count());
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
