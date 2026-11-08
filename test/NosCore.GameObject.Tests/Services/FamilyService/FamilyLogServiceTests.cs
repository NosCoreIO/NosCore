//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyLogServiceTests
    {
        private const long FamilyId = 3;
        private readonly List<FamilyLogDto> _logs = new();
        private FakeClock _clock = null!;
        private FamilyLogService _service = null!;
        private bool _hasWritten;

        [TestInitialize]
        public void Setup()
        {
            _logs.Clear();
            _clock = new FakeClock(Instant.FromUtc(2026, 11, 8, 12, 0));
            var dao = new Mock<IDao<FamilyLogDto, long>>();
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyLogDto>()))
                .Callback<FamilyLogDto>(_logs.Add)
                .ReturnsAsync((FamilyLogDto log) => log);
            dao.Setup(s => s.Where(It.IsAny<Expression<Func<FamilyLogDto, bool>>>()))
                .Returns((Expression<Func<FamilyLogDto, bool>> p) => _logs.Where(p.Compile()));
            _service = new FamilyLogService(dao.Object, _clock);
        }

        [TestMethod]
        public async Task AnEntryStoresItsArgumentsInOrderWithTheCurrentTime()
        {
            await new Spec("An entry keeps its type, its arguments in order and the time it was written")
                .WhenAsync(LoggingThatNewbieJoinedOnHeadsInvite)
                .Then(TheOnlyEntryShouldBe_, FamilyLogType.UserManaged, "Newbie|Head")
                .And(ItShouldBeStampedNow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOverlongEntryIsCutToTheColumn()
        {
            await new Spec("A daily message too long for the log column is cut instead of failing the write")
                .WhenAsync(LoggingADailyMessageOf_Characters, 300)
                .Then(TheOnlyEntryShouldBe_CharactersLong, 255)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryTenthLevelFromThirtyIsAnnounced()
        {
            await new Spec("Jumping from 29 to 31 announces level 30 only")
                .WhenAsync(LevellingFrom_To_, (byte)29, (byte)31)
                .Then(TheAnnouncedLevelsShouldBe_, new[] { "30" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LowLevelsAreNotAnnounced()
        {
            await new Spec("Levels up to 20 never reach the family history")
                .WhenAsync(LevellingFrom_To_, (byte)15, (byte)20)
                .Then(TheAnnouncedLevelsShouldBe_, Array.Empty<string>())
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EveryLevelAboveEightyIsAnnounced()
        {
            await new Spec("Every level past 80 is announced")
                .WhenAsync(LevellingFrom_To_, (byte)79, (byte)82)
                .Then(TheAnnouncedLevelsShouldBe_, new[] { "80", "81", "82" })
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADailyMessageFromTheLastDayCounts()
        {
            await new Spec("A daily message written 23 hours ago means today's is already written")
                .Given(_WroteADailyMessage_HoursAgo, "Head", 23)
                .WhenAsync(CheckingWhether_WroteToday, "Head")
                .Then(ItShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADailyMessageOlderThanADayDoesNotCount()
        {
            await new Spec("A daily message written 25 hours ago leaves today's free")
                .Given(_WroteADailyMessage_HoursAgo, "Head", 25)
                .WhenAsync(CheckingWhether_WroteToday, "Head")
                .Then(ItShouldBe_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherMembersDailyMessageDoesNotCount()
        {
            await new Spec("Someone else's daily message, even one whose name starts the same, does not count")
                .Given(_WroteADailyMessage_HoursAgo, "Headless", 1)
                .WhenAsync(CheckingWhether_WroteToday, "Head")
                .Then(ItShouldBe_, false)
                .ExecuteAsync();
        }

        private Task LoggingThatNewbieJoinedOnHeadsInvite() =>
            _service.AddAsync(FamilyId, FamilyLogType.UserManaged, "Newbie", "Head");

        private Task LoggingADailyMessageOf_Characters(int length) =>
            _service.AddAsync(FamilyId, FamilyLogType.DailyMessage, "Head", new string('a', length));

        private Task LevellingFrom_To_(byte previous, byte level) =>
            _service.AddLevelUpsAsync(FamilyId, "Head", previous, level);

        private void _WroteADailyMessage_HoursAgo(string name, int hours) => _logs.Add(new FamilyLogDto
        {
            FamilyId = FamilyId,
            FamilyLogType = FamilyLogType.DailyMessage,
            FamilyLogData = $"{name}|hello",
            Timestamp = _clock.GetCurrentInstant() - Duration.FromHours(hours)
        });

        private async Task CheckingWhether_WroteToday(string name) =>
            _hasWritten = await _service.HasWrittenDailyMessageTodayAsync(FamilyId, name);

        private void TheOnlyEntryShouldBe_(FamilyLogType type, string data)
        {
            var log = _logs.Single();
            Assert.AreEqual(FamilyId, log.FamilyId);
            Assert.AreEqual(type, log.FamilyLogType);
            Assert.AreEqual(data, log.FamilyLogData);
        }

        private void ItShouldBeStampedNow() => Assert.AreEqual(_clock.GetCurrentInstant(), _logs.Single().Timestamp);

        private void TheOnlyEntryShouldBe_CharactersLong(int length) => Assert.AreEqual(length, _logs.Single().FamilyLogData!.Length);

        private void TheAnnouncedLevelsShouldBe_(string[] levels)
        {
            Assert.IsTrue(_logs.All(s => s.FamilyLogType == FamilyLogType.LevelUp));
            CollectionAssert.AreEqual(levels.Select(l => $"Head|{l}").ToList(), _logs.Select(s => s.FamilyLogData).ToList());
        }

        private void ItShouldBe_(bool expected) => Assert.AreEqual(expected, _hasWritten);
    }
}
