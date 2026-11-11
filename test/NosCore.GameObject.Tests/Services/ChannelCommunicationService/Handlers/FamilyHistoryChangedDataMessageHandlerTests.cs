//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelCommunicationService.Handlers
{
    [TestClass]
    public class FamilyHistoryChangedDataMessageHandlerTests
    {
        private TestWorld World;
        private const long FamilyId = 42;
        private ClientSession _member;
        private ClientSession _outsider;
        private Mock<ISessionRegistry> _sessionRegistry;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _member = await World.CreateSessionAsync();
            _outsider = await World.CreateSessionAsync();
            _member.Character.Family = new NosCore.GameObject.Services.FamilyService.Family { FamilyId = FamilyId, Name = "Testers" };
            _outsider.Character.Family = new NosCore.GameObject.Services.FamilyService.Family { FamilyId = FamilyId + 1, Name = "Others" };
            var sessions = new[] { _member, _outsider };
            _sessionRegistry = World.Freeze<Mock<ISessionRegistry>>();
            _sessionRegistry.Setup(s => s.GetSessions(It.IsAny<Func<ClientSession, bool>?>()))
                .Returns((Func<ClientSession, bool>? predicate) => predicate == null ? sessions : sessions.Where(predicate));
            _member.LastPackets.Clear();
            _outsider.LastPackets.Clear();
        }

        [TestMethod]
        public async Task OnlyTheFamilyIsToldToRefreshItsHistory()
        {
            await new Spec("A history change tells the family's online members to ask again, and nobody else")
                .WhenAsync(TheFamilysHistoryChanges)
                .Then(TheMemberShouldBeToldToRefresh)
                .And(TheOutsiderShouldNotBeTold)
                .ExecuteAsync();
        }

        private Task TheFamilysHistoryChanges() =>
            World.Create<FamilyHistoryChangedDataMessageChannelCommunicationMessageHandler>()
                .Handle(new FamilyHistoryChangedData { FamilyId = FamilyId });

        private void TheMemberShouldBeToldToRefresh() =>
            Assert.AreEqual(1, _member.LastPackets.OfType<FhisStcPacket>().Count());

        private void TheOutsiderShouldNotBeTold() =>
            Assert.IsFalse(_outsider.LastPackets.OfType<FhisStcPacket>().Any());
    }
}
