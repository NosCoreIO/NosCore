//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Services.BroadcastService;
using Serilog;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.BroadcastService
{
    [TestClass]
    public class SessionRegistryTests
    {
        private ISessionRegistry Registry = null!;
        private Mock<ILogger> MockLogger = null!;

        [TestInitialize]
        public void Setup()
        {
            MockLogger = new Mock<ILogger>();
            Registry = new SessionRegistry(MockLogger.Object);
        }

        [TestMethod]
        public async Task RegisterShouldAddSession()
        {
            await new Spec("Register should add session")
                .When(RegisteringSession)
                .Then(SessionShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterShouldRemoveSession()
        {
            await new Spec("Unregister should remove session")
                .Given(SessionIsRegistered)
                .When(UnregisteringSession)
                .Then(SessionShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpdateCharacterShouldAssociateCharacterWithSession()
        {
            await new Spec("Update character should associate character with session")
                .Given(SessionIsRegistered)
                .When(UpdatingCharacter)
                .Then(CharacterShouldBeAssociated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetSenderByCharacterIdShouldReturnNullForUnknownCharacter()
        {
            await new Spec("Get sender by character ID should return null for unknown character")
                .When(GettingUnknownCharacterSender)
                .Then(SenderShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAllSessionsShouldReturnAllSessions()
        {
            await new Spec("Get all sessions should return all sessions")
                .Given(MultipleSessionsAreRegistered)
                .When(GettingAllSessions)
                .Then(AllSessionsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetSessionsByMapInstanceShouldFilterByMapInstance()
        {
            await new Spec("Get sessions by map instance should filter by map instance")
                .Given(SessionsWithDifferentMapsAreRegistered)
                .When(GettingSessionsByMapInstance)
                .Then(OnlyMatchingSessionsShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetConnectedAccountsShouldReturnSubscriberList()
        {
            await new Spec("Get connected accounts should return subscriber list")
                .Given(SessionIsRegistered)
                .When(GettingConnectedAccounts)
                .Then(SubscriberListShouldBeReturned)
                .ExecuteAsync();
        }

        private const string TestChannelId = "channel-1";
        private const string TestChannelId2 = "channel-2";
        private const long TestCharacterId = 100;
        private readonly Guid TestMapInstanceId = Guid.NewGuid();
        private readonly Guid TestMapInstanceId2 = Guid.NewGuid();
        private Mock<IPacketSender> MockSender = null!;
        private IPacketSender? ResultSender;
        private int SessionCount;
        private int FilteredSessionCount;
        private int SubscriberCount;

        private void RegisteringSession()
        {
            MockSender = new Mock<IPacketSender>();
            Registry.Register(new SessionInfo
            {
                ChannelId = TestChannelId,
                SessionId = 1,
                Sender = MockSender.Object,
                AccountName = "TestAccount",
                Disconnect = () => Task.CompletedTask
            });
        }

        private void SessionIsRegistered()
        {
            RegisteringSession();
        }

        private void MultipleSessionsAreRegistered()
        {
            MockSender = new Mock<IPacketSender>();
            Registry.Register(new SessionInfo { ChannelId = TestChannelId, SessionId = 1, Sender = MockSender.Object, AccountName = "Account1", Disconnect = () => Task.CompletedTask });
            Registry.Register(new SessionInfo { ChannelId = TestChannelId2, SessionId = 2, Sender = MockSender.Object, AccountName = "Account2", Disconnect = () => Task.CompletedTask });
        }

        private void SessionsWithDifferentMapsAreRegistered()
        {
            MockSender = new Mock<IPacketSender>();
            Registry.Register(new SessionInfo { ChannelId = TestChannelId, SessionId = 1, Sender = MockSender.Object, AccountName = "Account1", Disconnect = () => Task.CompletedTask });
            Registry.Register(new SessionInfo { ChannelId = TestChannelId2, SessionId = 2, Sender = MockSender.Object, AccountName = "Account2", Disconnect = () => Task.CompletedTask });
            Registry.UpdateCharacter(TestChannelId, TestCharacterId, TestMapInstanceId, null);
            Registry.UpdateCharacter(TestChannelId2, TestCharacterId + 1, TestMapInstanceId2, null);
        }

        private void UnregisteringSession()
        {
            Registry.Unregister(TestChannelId);
        }

        private void UpdatingCharacter()
        {
            Registry.UpdateCharacter(TestChannelId, TestCharacterId, TestMapInstanceId, null);
        }

        private void GettingUnknownCharacterSender()
        {
            ResultSender = Registry.GetSenderByCharacterId(9999);
        }

        private void GettingAllSessions()
        {
            SessionCount = Registry.GetAllSessions().Count();
        }

        private void GettingSessionsByMapInstance()
        {
            FilteredSessionCount = Registry.GetSessionsByMapInstance(TestMapInstanceId).Count();
        }

        private void GettingConnectedAccounts()
        {
            SubscriberCount = Registry.GetConnectedAccounts().Count;
        }

        private void SessionShouldBeRetrievable()
        {
            var sender = Registry.GetSenderByChannelId(TestChannelId);
            Assert.IsNotNull(sender);
        }

        private void SessionShouldNotExist()
        {
            var sender = Registry.GetSenderByChannelId(TestChannelId);
            Assert.IsNull(sender);
        }

        private void CharacterShouldBeAssociated()
        {
            var sender = Registry.GetSenderByCharacterId(TestCharacterId);
            Assert.IsNotNull(sender);
        }

        private void SenderShouldBeNull()
        {
            Assert.IsNull(ResultSender);
        }

        private void AllSessionsShouldBeReturned()
        {
            Assert.AreEqual(2, SessionCount);
        }

        private void OnlyMatchingSessionsShouldBeReturned()
        {
            Assert.AreEqual(1, FilteredSessionCount);
        }

        private void SubscriberListShouldBeReturned()
        {
            Assert.AreEqual(1, SubscriberCount);
        }
    }
}
