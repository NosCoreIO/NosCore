//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.MinilandService;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MinilandService
{
    [TestClass]
    public class MinilandPointExtensionsTests
    {
        private MinilandDto _miniland = new();
        private Instant _now;
        private long? _earned;

        [TestInitialize]
        public void Setup()
        {
            _miniland = new MinilandDto();
            _now = Instant.FromUtc(2026, 11, 8, 23, 30);
        }

        [TestMethod]
        public void ANewMinilandShouldReceiveTheDailyAllowance()
        {
            new Spec("A new miniland should receive the daily allowance")
                .When(PointsAreRefreshed)
                .Then(PointsShouldBe_, 2000L)
                .Execute();
        }

        [TestMethod]
        public void SpentPointsShouldNotComeBackTheSameDay()
        {
            new Spec("Spent points should not come back the same day")
                .Given(PointsAreRefreshed)
                .And(PointsAreSpentDownTo_, 300L)
                .And(MinutesPass_, 29)
                .When(PointsAreRefreshed)
                .Then(PointsShouldBe_, 300L)
                .Execute();
        }

        [TestMethod]
        public void PointsAndDailyVisitsShouldResetAtMidnightUtc()
        {
            new Spec("Points and daily visits should reset at midnight UTC")
                .Given(PointsAreRefreshed)
                .And(PointsAreSpentDownTo_, 300L)
                .And(OwnerHadDailyVisits_, 4)
                .And(MinutesPass_, 31)
                .When(PointsAreRefreshed)
                .Then(PointsShouldBe_, 2000L)
                .And(DailyVisitsShouldBe_, 0)
                .Execute();
        }

        [TestMethod]
        public void TheDaysVisitsShouldEarnTheOwnerReputationAtMidnight()
        {
            new Spec("The day's visits should earn the owner reputation at midnight")
                .Given(PointsAreRefreshed)
                .And(OwnerHadDailyVisits_, 4)
                .And(MinutesPass_, 31)
                .When(PointsAreRefreshed)
                .Then(EarnedReputationShouldBe_, 8L)
                .Execute();
        }

        [TestMethod]
        public void VisitsShouldEarnNothingBeforeMidnight()
        {
            new Spec("Visits should earn nothing before midnight")
                .Given(PointsAreRefreshed)
                .And(OwnerHadDailyVisits_, 4)
                .And(MinutesPass_, 29)
                .When(PointsAreRefreshed)
                .Then(NoReputationShouldBeEarned)
                .And(DailyVisitsShouldBe_, 4)
                .Execute();
        }

        [TestMethod]
        public void PointsShouldNotExceedTheDailyAllowance()
        {
            new Spec("Points should not exceed the daily allowance")
                .Given(PointsAreRefreshed)
                .And(MinutesPass_, 31)
                .When(PointsAreRefreshed)
                .Then(PointsShouldBe_, 2000L)
                .Execute();
        }

        private void PointsAreRefreshed() => _earned = _miniland.RefreshDailyPoints(_now);

        private void EarnedReputationShouldBe_(long reputation) => Assert.AreEqual(reputation, _earned);

        private void NoReputationShouldBeEarned() => Assert.IsNull(_earned);

        private void PointsAreSpentDownTo_(long points) => _miniland.MinilandPoint = points;

        private void OwnerHadDailyVisits_(int visits) => _miniland.DailyVisitCount = visits;

        private void MinutesPass_(int minutes) => _now += Duration.FromMinutes(minutes);

        private void PointsShouldBe_(long points) => Assert.AreEqual(points, _miniland.MinilandPoint);

        private void DailyVisitsShouldBe_(int visits) => Assert.AreEqual(visits, _miniland.DailyVisitCount);
    }
}
