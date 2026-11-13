//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyQuestPeriodTests
    {
        private static readonly Instant Yesterday = Instant.FromUtc(2026, 11, 12, 23, 0);
        private static readonly Instant Today = Instant.FromUtc(2026, 11, 13, 1, 0);

        private static FamilyQuestDto Quest(bool daily) => new()
        {
            QuestId = 9017, Kind = FamilyQuestKind.Mission, Target = 10, ResetsDaily = daily
        };

        private static FamilyQuestProgressDto Progress() => new()
        {
            FamilyId = 1, QuestId = 9017, Progress = 10, CompletedAt = Yesterday, TotalCompletions = 3, ProgressedAt = Yesterday
        };

        [TestMethod]
        public void YesterdaysDailyMissionShowsAsNotStarted()
        {
            var current = FamilyQuestPeriod.Current(Quest(daily: true), Progress(), Today);

            Assert.AreEqual(0, current.Progress);
            Assert.IsNull(current.CompletedAt);
            Assert.AreEqual(3, current.TotalCompletions);
        }

        [TestMethod]
        public void AQuestThatDoesNotResetKeepsItsProgress()
        {
            var current = FamilyQuestPeriod.Current(Quest(daily: false), Progress(), Today);

            Assert.AreEqual(10, current.Progress);
            Assert.AreEqual(Yesterday, current.CompletedAt);
        }

        [TestMethod]
        public void TodaysDailyMissionKeepsItsProgress()
        {
            Assert.IsFalse(FamilyQuestPeriod.HasExpired(Quest(daily: true), Progress(), Yesterday.Plus(Duration.FromMinutes(30))));
        }
    }
}
