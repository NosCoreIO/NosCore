//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Tests.Shared;
using PenaltyServiceImpl = NosCore.GameObject.Services.PenaltyService.PenaltyService;

namespace NosCore.GameObject.Tests.Services.PenaltyService
{
    [TestClass]
    public class PenaltyServiceTests
    {
        private const long AccountId = 7;

        private TestWorld World;
        private PenaltyServiceImpl _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<PenaltyServiceImpl>();
        }

        [TestMethod]
        public async Task AnAccountWithoutPenaltiesMaySpeakAndLogIn()
        {
            Assert.IsNull(await _service.GetMuteLeftAsync(AccountId));
            Assert.IsFalse(await _service.IsBannedAsync(AccountId));
        }

        [TestMethod]
        public async Task ARunningMuteSaysHowLongIsLeft()
        {
            await GiveAsync(PenaltyType.Muted, Duration.FromMinutes(90));

            World.Clock.Advance(Duration.FromMinutes(30));

            Assert.AreEqual(Duration.FromMinutes(60), await _service.GetMuteLeftAsync(AccountId));
        }

        [TestMethod]
        public async Task AMuteThatHasEndedIsGone()
        {
            await GiveAsync(PenaltyType.Muted, Duration.FromMinutes(10));

            World.Clock.Advance(Duration.FromMinutes(10));

            Assert.IsNull(await _service.GetMuteLeftAsync(AccountId));
        }

        [TestMethod]
        public async Task TheLongestOfSeveralMutesCounts()
        {
            await GiveAsync(PenaltyType.Muted, Duration.FromMinutes(10));
            await GiveAsync(PenaltyType.Muted, Duration.FromHours(2));
            await GiveAsync(PenaltyType.Muted, Duration.FromMinutes(30));

            Assert.AreEqual(Duration.FromHours(2), await _service.GetMuteLeftAsync(AccountId));
        }

        [TestMethod]
        public async Task AMuteIsNotABanAndABanIsNotAMute()
        {
            await GiveAsync(PenaltyType.Muted, Duration.FromHours(1));
            Assert.IsFalse(await _service.IsBannedAsync(AccountId));

            await GiveAsync(PenaltyType.Banned, Duration.FromHours(1), accountId: 8);
            Assert.IsNull(await _service.GetMuteLeftAsync(8));
            Assert.IsTrue(await _service.IsBannedAsync(8));
        }

        [TestMethod]
        public async Task ABanEndsOnItsDate()
        {
            await GiveAsync(PenaltyType.Banned, Duration.FromDays(1));
            Assert.IsTrue(await _service.IsBannedAsync(AccountId));

            World.Clock.Advance(Duration.FromDays(1));
            Assert.IsFalse(await _service.IsBannedAsync(AccountId));
        }

        [TestMethod]
        public async Task AnotherAccountsPenaltyDoesNotCount()
        {
            await GiveAsync(PenaltyType.Muted, Duration.FromHours(1), accountId: 8);
            await GiveAsync(PenaltyType.Banned, Duration.FromHours(1), accountId: 8);

            Assert.IsNull(await _service.GetMuteLeftAsync(AccountId));
            Assert.IsFalse(await _service.IsBannedAsync(AccountId));
        }

        private Task GiveAsync(PenaltyType penalty, Duration length, long accountId = AccountId)
        {
            var now = World.Clock.GetCurrentInstant();
            return World.PenaltyLogDao.TryInsertOrUpdateAsync(new PenaltyLogDto
            {
                AccountId = accountId,
                Penalty = penalty,
                DateStart = now,
                DateEnd = now.Plus(length),
                AdminName = "admin",
                Reason = "test",
            });
        }
    }
}
