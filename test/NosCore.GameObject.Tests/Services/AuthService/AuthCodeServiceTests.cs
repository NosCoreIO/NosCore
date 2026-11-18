//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NodaTime.Testing;
using NosCore.GameObject.Services.AuthService;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.AuthService
{
    [TestClass]
    public class AuthCodeServiceTests
    {
        private const string TestAuthCode = "test-auth-code";
        private const string TestAccountName = "TestAccount";
        private const long TestSessionId = 12345;
        private const long TestChannelId = 2;

        private FakeClock Clock;
        private IAuthCodeService Service;
        private string? ConsumedAccountName;
        private WorldEntryGrant? Grant;

        [TestInitialize]
        public void Setup()
        {
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            Service = new AuthCodeService(Clock);
            ConsumedAccountName = null;
            Grant = null;
        }

        [TestMethod]
        public async Task StoredAuthCodeCanBeConsumed()
        {
            await new Spec("Stored auth code can be consumed")
                .Given(AuthCodeIsStored)
                .When(ConsumingAuthCode)
                .Then(ConsumedAccountShouldBeTheStoredOne)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AuthCodeCanOnlyBeConsumedOnce()
        {
            await new Spec("Auth code can only be consumed once")
                .Given(AuthCodeIsStored)
                .And(AuthCodeWasConsumed)
                .When(ConsumingAuthCode)
                .Then(NoAccountShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExpiredAuthCodeIsRefused()
        {
            await new Spec("Expired auth code is refused")
                .Given(AuthCodeIsStored)
                .And(AuthCodeLifetimeElapses)
                .When(ConsumingAuthCode)
                .Then(NoAccountShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownAuthCodeIsRefused()
        {
            await new Spec("Unknown auth code is refused")
                .When(ConsumingAuthCode)
                .Then(NoAccountShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReadyForAuthGrantsEntryWithMfaValidated()
        {
            await new Spec("Ready for auth grants entry with MFA validated")
                .Given(AccountIsMarkedReady)
                .When(ConsumingReadyForAuth)
                .Then(EntryShouldBeGrantedWithMfa)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReadyForAuthIsRefusedForAnotherSession()
        {
            await new Spec("Ready for auth is refused for another session")
                .Given(AccountIsMarkedReady)
                .When(ConsumingReadyForAuthWithAnotherSession)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReadyForAuthCanOnlyBeConsumedOnce()
        {
            await new Spec("Ready for auth can only be consumed once")
                .Given(AccountIsMarkedReady)
                .And(ReadyForAuthWasConsumed)
                .When(ConsumingReadyForAuth)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReadyForAuthStaysAvailableAfterAWrongSession()
        {
            await new Spec("Ready for auth stays available after a wrong session")
                .Given(AccountIsMarkedReady)
                .And(ReadyForAuthWasTriedWithAnotherSession)
                .When(ConsumingReadyForAuth)
                .Then(EntryShouldBeGrantedWithMfa)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReadyForAuthOutlastsAPlayerLingeringOnTheChannelList()
        {
            await new Spec("Ready for auth outlasts a player lingering on the channel list")
                .Given(AccountIsMarkedReady)
                .And(ThePlayerLingersOnTheChannelList)
                .When(ConsumingReadyForAuth)
                .Then(EntryShouldBeGrantedWithMfa)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExpiredReadyForAuthIsRefused()
        {
            await new Spec("Expired ready for auth is refused")
                .Given(AccountIsMarkedReady)
                .And(ReadyForAuthLifetimeElapses)
                .When(ConsumingReadyForAuth)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeGrantsEntryOnTheTargetChannel()
        {
            await new Spec("Channel change grants entry on the target channel")
                .Given(AccountIsChangingChannelWithoutMfa)
                .When(ConsumingChannelChangeOnTarget)
                .Then(EntryShouldBeGrantedWithoutMfa)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeIsRefusedOnAnotherChannel()
        {
            await new Spec("Channel change is refused on another channel")
                .Given(AccountIsChangingChannelWithoutMfa)
                .When(ConsumingChannelChangeOnAnotherChannel)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeCanOnlyBeConsumedOnce()
        {
            await new Spec("Channel change can only be consumed once")
                .Given(AccountIsChangingChannelWithoutMfa)
                .And(ChannelChangeWasConsumed)
                .When(ConsumingChannelChangeOnTarget)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ExpiredChannelChangeIsRefused()
        {
            await new Spec("Expired channel change is refused")
                .Given(AccountIsChangingChannelWithoutMfa)
                .And(ChannelChangeLifetimeElapses)
                .When(ConsumingChannelChangeOnTarget)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeDoesNotGrantALoginEntry()
        {
            await new Spec("Channel change does not grant a login entry")
                .Given(AccountIsChangingChannelWithoutMfa)
                .When(ConsumingReadyForAuthWithChannelIdAsSession)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        private void AuthCodeIsStored() => Service.StoreAuthCode(TestAuthCode, TestAccountName);

        private void AuthCodeWasConsumed() => Service.ConsumeAuthCode(TestAuthCode);

        private void AuthCodeLifetimeElapses() => Clock.Advance(AuthCodeService.AuthCodeLifetime);

        private void ConsumingAuthCode() => ConsumedAccountName = Service.ConsumeAuthCode(TestAuthCode);

        private void AccountIsMarkedReady() => Service.MarkReadyForAuth(TestAccountName, TestSessionId);

        private void ReadyForAuthWasConsumed() => Service.ConsumeReadyForAuth(TestAccountName, TestSessionId);

        private void ReadyForAuthWasTriedWithAnotherSession() => Service.ConsumeReadyForAuth(TestAccountName, TestSessionId + 2);

        private void ThePlayerLingersOnTheChannelList() => Clock.Advance(Duration.FromMinutes(5));

        private void ReadyForAuthLifetimeElapses() => Clock.Advance(AuthCodeService.ReadyForAuthLifetime);

        private void ConsumingReadyForAuth() => Grant = Service.ConsumeReadyForAuth(TestAccountName, TestSessionId);

        private void ConsumingReadyForAuthWithAnotherSession() =>
            Grant = Service.ConsumeReadyForAuth(TestAccountName, TestSessionId + 2);

        private void ConsumingReadyForAuthWithChannelIdAsSession() =>
            Grant = Service.ConsumeReadyForAuth(TestAccountName, TestChannelId);

        private void AccountIsChangingChannelWithoutMfa() =>
            Service.MarkReadyForChannelChange(TestAccountName, TestChannelId, false);

        private void ChannelChangeWasConsumed() => Service.ConsumeChannelChange(TestAccountName, TestChannelId);

        private void ChannelChangeLifetimeElapses() => Clock.Advance(AuthCodeService.ChannelChangeLifetime);

        private void ConsumingChannelChangeOnTarget() => Grant = Service.ConsumeChannelChange(TestAccountName, TestChannelId);

        private void ConsumingChannelChangeOnAnotherChannel() =>
            Grant = Service.ConsumeChannelChange(TestAccountName, TestChannelId + 1);

        private void ConsumedAccountShouldBeTheStoredOne() => Assert.AreEqual(TestAccountName, ConsumedAccountName);

        private void NoAccountShouldBeReturned() => Assert.IsNull(ConsumedAccountName);

        private void EntryShouldBeGrantedWithMfa() => Assert.AreEqual(new WorldEntryGrant(true), Grant);

        private void EntryShouldBeGrantedWithoutMfa() => Assert.AreEqual(new WorldEntryGrant(false), Grant);

        private void EntryShouldBeRefused() => Assert.IsNull(Grant);
    }
}
