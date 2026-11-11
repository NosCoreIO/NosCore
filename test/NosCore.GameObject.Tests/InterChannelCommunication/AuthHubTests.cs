//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NodaTime.Testing;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Services.AuthService;
using SpecLight;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.InterChannelCommunication
{
    [TestClass]
    public class AuthHubTests
    {
        private const string AccountName = "Account";
        private readonly string TokenGuid = Guid.NewGuid().ToString();
        private IAuthCodeService AuthCodeService;
        private AuthHub Hub;
        private const string ChannelKey = "world-channel-2";
        private const string OtherChannelKey = "world-channel-3";
        private string? Result;
        private WorldEntryGrant? Grant;

        [TestInitialize]
        public void Setup()
        {
            AuthCodeService = new AuthCodeService(new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)));
            var masterClientList = new MasterClientList();
            masterClientList.Channels[ChannelKey] = new ChannelInfo { Host = "127.0.0.1", Id = 2 };
            masterClientList.Channels[OtherChannelKey] = new ChannelInfo { Host = "127.0.0.1", Id = 3 };
            Hub = new AuthHub(AuthCodeService, masterClientList);
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenTokenNull()
        {
            await new Spec("Get awaiting connection should return null when token null")
                .WhenAsync(GettingAwaitingConnectionWithNullToken)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenNoAuthCode()
        {
            await new Spec("Get awaiting connection should return null when no auth code")
                .WhenAsync(GettingAwaitingConnectionWithInvalidAuthCode)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnAccountNameWhenAuthCode()
        {
            await new Spec("Get awaiting connection should return account name when auth code")
                .Given(AuthCodeIsStored)
                .WhenAsync(GettingAwaitingConnectionWithValidAuthCode)
                .Then(ShouldReturnAccountName)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetAwaitingConnectionShouldReturnNullWhenNoneSessionTicket()
        {
            await new Spec("Get awaiting connection should return null when none session ticket")
                .WhenAsync(GettingAwaitingConnectionWithNoneSessionTicket)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AuthCodeIsAcceptedOnlyOnce()
        {
            await new Spec("Auth code is accepted only once")
                .Given(AuthCodeIsStored)
                .AndAsync(AuthCodeWasRedeemed)
                .WhenAsync(GettingAwaitingConnectionWithValidAuthCode)
                .Then(ShouldReturnNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RedeemedAuthCodeLetsTheIssuedSessionEnterTheWorld()
        {
            await new Spec("Redeemed auth code lets the issued session enter the world")
                .Given(AuthCodeIsStored)
                .AndAsync(AuthCodeWasRedeemed)
                .WhenAsync(RedeemingWorldEntry)
                .Then(EntryShouldBeGranted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WorldEntryIsRefusedForASessionLoginDidNotIssue()
        {
            await new Spec("World entry is refused for a session login did not issue")
                .Given(AuthCodeIsStored)
                .AndAsync(AuthCodeWasRedeemed)
                .WhenAsync(RedeemingWorldEntryWithAnotherSession)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AuthCodeIsAcceptedOnceAmongConcurrentRequests()
        {
            await new Spec("Auth code is accepted once among concurrent requests")
                .Given(AuthCodeIsStored)
                .WhenAsync(SixteenRequestsRedeemTheAuthCodeAtOnce)
                .Then(ExactlyOneRequestShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WorldEntryIsGrantedOnceAmongConcurrentRequests()
        {
            await new Spec("World entry is granted once among concurrent requests")
                .Given(AuthCodeIsStored)
                .AndAsync(AuthCodeWasRedeemed)
                .WhenAsync(SixteenRequestsRedeemTheWorldEntryAtOnce)
                .Then(ExactlyOneRequestShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeIsGrantedOnceAmongConcurrentRequests()
        {
            await new Spec("Channel change is granted once among concurrent requests")
                .GivenAsync(AccountIsMovingToChannel2)
                .WhenAsync(SixteenRequestsRedeemTheChannelChangeAtOnce)
                .Then(ExactlyOneRequestShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WorldEntryIsGrantedOnlyOnce()
        {
            await new Spec("World entry is granted only once")
                .Given(AuthCodeIsStored)
                .AndAsync(AuthCodeWasRedeemed)
                .AndAsync(WorldEntryWasRedeemed)
                .WhenAsync(RedeemingWorldEntry)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WorldEntryIsRefusedWithoutLogin()
        {
            await new Spec("World entry is refused without login")
                .WhenAsync(RedeemingWorldEntry)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeIsGrantedOnTheTargetChannel()
        {
            await new Spec("Channel change is granted on the target channel")
                .GivenAsync(AccountIsMovingToChannel2)
                .When(RedeemingChannelChangeOnChannel2)
                .Then(EntryShouldBeGranted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeIsRefusedOnAnotherChannel()
        {
            await new Spec("Channel change is refused on another channel")
                .GivenAsync(AccountIsMovingToChannel2)
                .When(RedeemingChannelChangeOnChannel3)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ChannelChangeIsGrantedOnlyOnce()
        {
            await new Spec("Channel change is granted only once")
                .GivenAsync(AccountIsMovingToChannel2)
                .And(RedeemingChannelChangeOnChannel2)
                .When(RedeemingChannelChangeOnChannel2)
                .Then(EntryShouldBeRefused)
                .ExecuteAsync();
        }

        private void AuthCodeIsStored()
        {
            AuthCodeService.StoreAuthCode(TokenGuid, AccountName);
        }

        private int Successes;

        private async Task<int> CountSuccessesAsync(Func<Task<bool>> redeem)
        {
            using var start = new ManualResetEventSlim(false);
            var requests = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() =>
                {
                    start.Wait();
                    return redeem();
                }))
                .ToArray();
            start.Set();
            return (await Task.WhenAll(requests)).Count(success => success);
        }

        private async Task SixteenRequestsRedeemTheAuthCodeAtOnce() =>
            Successes = await CountSuccessesAsync(async () =>
                await Hub.GetAwaitingConnectionAsync(AccountName, TokenGuid, 1) != null);

        private async Task SixteenRequestsRedeemTheWorldEntryAtOnce() =>
            Successes = await CountSuccessesAsync(async () => await Hub.RedeemWorldEntryAsync(AccountName, 1) != null);

        private async Task SixteenRequestsRedeemTheChannelChangeAtOnce() =>
            Successes = await CountSuccessesAsync(() => Task.FromResult(Hub.RedeemChannelChange(ChannelKey, AccountName) != null));

        private void ExactlyOneRequestShouldSucceed() => Assert.AreEqual(1, Successes);

        private async Task AuthCodeWasRedeemed()
        {
            await GettingAwaitingConnectionWithValidAuthCode();
        }

        private async Task WorldEntryWasRedeemed()
        {
            await RedeemingWorldEntry();
        }

        private async Task RedeemingWorldEntry()
        {
            Grant = await Hub.RedeemWorldEntryAsync(AccountName, 1);
        }

        private async Task RedeemingWorldEntryWithAnotherSession()
        {
            Grant = await Hub.RedeemWorldEntryAsync(AccountName, 3);
        }

        private Task AccountIsMovingToChannel2()
        {
            return Hub.SetAwaitingChannelChangeAsync(AccountName, 2, true);
        }

        private void RedeemingChannelChangeOnChannel2()
        {
            Grant = Hub.RedeemChannelChange(ChannelKey, AccountName);
        }

        private void RedeemingChannelChangeOnChannel3()
        {
            Grant = Hub.RedeemChannelChange(OtherChannelKey, AccountName);
        }

        private async Task GettingAwaitingConnectionWithNullToken()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, null, 1);
        }

        private async Task GettingAwaitingConnectionWithInvalidAuthCode()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "A1A2A3", 1);
        }

        private async Task GettingAwaitingConnectionWithValidAuthCode()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, string.Join("", TokenGuid.ToCharArray().Select(s => Convert.ToByte(s).ToString("x"))), 1);
        }

        private async Task GettingAwaitingConnectionWithNoneSessionTicket()
        {
            Result = await Hub.GetAwaitingConnectionAsync(AccountName, "NONE_SESSION_TICKET", 1);
        }

        private void ShouldReturnNull()
        {
            Assert.IsNull(Result);
        }

        private void ShouldReturnAccountName()
        {
            Assert.AreEqual(AccountName, Result);
        }

        private void EntryShouldBeGranted()
        {
            Assert.AreEqual(new WorldEntryGrant(true), Grant);
        }

        private void EntryShouldBeRefused()
        {
            Assert.IsNull(Grant);
        }
    }
}
