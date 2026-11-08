//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class PubSubHubSubscriptionTests
    {
        private const string Channel1 = "world-1";
        private const string Channel2 = "world-2";

        private MasterClientList Clients = null!;
        private PubSubHub Hub = null!;
        private bool Result;
        private bool[] RaceResults = [];

        [TestInitialize]
        public void Setup()
        {
            Clients = new MasterClientList();
            foreach (var (key, id) in new[] { (Channel1, 1), (Channel2, 2) })
            {
                Clients.Channels[key] = new ChannelInfo { Id = id, Type = ServerType.WorldServer };
                Clients.ConnectedAccounts[key] = new ConcurrentDictionary<long, Subscriber>();
            }

            Hub = new PubSubHub(Clients, new Mock<IMessageBus>().Object, new MessagingConfiguration());
        }

        [TestMethod]
        public async Task FirstSessionOfAnAccountIsAccepted()
        {
            await new Spec("First session of an account is accepted")
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .Then(TheSubscriptionShouldBeAccepted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SecondSessionOfTheSameAccountIsRefused()
        {
            await new Spec("Second session of the same account is refused")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 4L, Channel2)
                .Then(TheSubscriptionShouldBeRefused)
                .And(Channel_ShouldHoldNoSubscriber, Channel2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyOneOfTwoConcurrentSessionsOfAnAccountIsAccepted()
        {
            await new Spec("Only one of two concurrent sessions of an account is accepted")
                .WhenAsync(TwoSessionsOfTheSameAccountSubscribeAtOnce)
                .Then(ExactlyOneSubscriptionShouldBeAccepted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameSessionCanUpdateItsSubscription()
        {
            await new Spec("The same session can update its subscription")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .Then(TheSubscriptionShouldBeAccepted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASessionIdHeldByAnotherAccountIsRefused()
        {
            await new Spec("A session id held by another account is refused")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .When(SubscribingAccount_WithSession_OnChannel_, "Bob", 2L, Channel1)
                .Then(TheSubscriptionShouldBeRefused)
                .And(Session_OnChannel_ShouldStillBelongTo_, 2L, Channel1, "Alice")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnsubscribingReleasesTheAccount()
        {
            await new Spec("Unsubscribing releases the account")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .And(Session_IsUnsubscribedFromChannel_, 2L, Channel1)
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 4L, Channel2)
                .Then(TheSubscriptionShouldBeAccepted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnsubscribingAnotherIdKeepsTheAccountClaimed()
        {
            await new Spec("Unsubscribing another id keeps the account claimed")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .And(Session_IsUnsubscribedFromChannel_, 2L, Channel2)
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 4L, Channel2)
                .Then(TheSubscriptionShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingAChannelReleasesItsAccounts()
        {
            await new Spec("Removing a channel releases its accounts")
                .Given(AccountIsSubscribed_WithSession_OnChannel_, "Alice", 2L, Channel1)
                .And(Channel_IsRemoved, Channel1)
                .When(SubscribingAccount_WithSession_OnChannel_, "Alice", 4L, Channel2)
                .Then(TheSubscriptionShouldBeAccepted)
                .ExecuteAsync();
        }

        private void AccountIsSubscribed_WithSession_OnChannel_(string name, long sessionId, string channel) =>
            Assert.IsTrue(Hub.Subscribe(channel, new Subscriber { Id = sessionId, Name = name }));

        private void SubscribingAccount_WithSession_OnChannel_(string name, long sessionId, string channel) =>
            Result = Hub.Subscribe(channel, new Subscriber { Id = sessionId, Name = name });

        private void Session_IsUnsubscribedFromChannel_(long sessionId, string channel) => Hub.Unsubscribe(channel, sessionId);

        private void Channel_IsRemoved(string channel) =>
            new ChannelHub(NullLogger<ChannelHub>.Instance, Clients, TestHelpers.Instance.LogLanguageLocalizer,
                new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0))).RemoveChannel(channel);

        private async Task TwoSessionsOfTheSameAccountSubscribeAtOnce()
        {
            using var barrier = new Barrier(2);
            var first = Task.Run(() =>
            {
                barrier.SignalAndWait();
                return Hub.Subscribe(Channel1, new Subscriber { Id = 2, Name = "Alice" });
            });
            var second = Task.Run(() =>
            {
                barrier.SignalAndWait();
                return Hub.Subscribe(Channel2, new Subscriber { Id = 4, Name = "Alice" });
            });
            RaceResults = await Task.WhenAll(first, second);
        }

        private void TheSubscriptionShouldBeAccepted() => Assert.IsTrue(Result);

        private void TheSubscriptionShouldBeRefused() => Assert.IsFalse(Result);

        private void ExactlyOneSubscriptionShouldBeAccepted()
        {
            Assert.AreEqual(1, RaceResults.Count(r => r));
            Assert.AreEqual(1, Clients.ConnectedAccounts.Values.Sum(c => c.Count));
        }

        private void Channel_ShouldHoldNoSubscriber(string channel) => Assert.AreEqual(0, Clients.ConnectedAccounts[channel].Count);

        private void Session_OnChannel_ShouldStillBelongTo_(long sessionId, string channel, string name) =>
            Assert.AreEqual(name, Clients.ConnectedAccounts[channel][sessionId].Name);
    }
}
