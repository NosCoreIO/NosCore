//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class FamilyQuestParserTests
    {
        private string _folder;
        private readonly List<FamilyQuestDto> _saved = new();
        private FamilyQuestParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            var dao = new Mock<IDao<FamilyQuestDto, short>>();
            dao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<FamilyQuestDto>>()))
                .Callback<IEnumerable<FamilyQuestDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new FamilyQuestParser(dao.Object, NullLoggerFactory.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private static string Quest(short id, string vnumTail, int level = -1, string data = "-1\t-1",
            string prize = "-1\t-1\t-1\t-1", int link = -1) =>
            "BEGIN\r\n" +
            $"VNUM\t{id}\t{vnumTail}\r\n" +
            $"LEVEL\t{level}\t-1\r\n" +
            "TITLE\tQuest\r\n" +
            "DESC\tQuest\r\n" +
            "TALK\t-1\t-1\t0\t0\r\n" +
            "TARGET\t-1\t-1\t-1\r\n" +
            $"DATA\t{data}\t-1\t-1\r\n" +
            $"PRIZE\t{prize}\r\n" +
            $"LINK\t{link}\r\n" +
            "END";

        private Task ParseAsync(params string[] quests)
        {
            File.WriteAllText(Path.Combine(_folder, "quest.dat"), string.Join("\r\n", quests));
            return _parser.InsertFamilyQuestsAsync(_folder);
        }

        [TestMethod]
        public async Task OnlyFamilyQuestRangesAreStored()
        {
            await ParseAsync(
                Quest(1, "-1\t0\t1\t4\t0"),
                Quest(9000, "-1\t0\t1\t4\t0"),
                Quest(9200, "-1\t0\t1\t4\t0"),
                Quest(9400, "-1\t1\t0\t0\t0"),
                Quest(9500, "-1\t1\t0\t0\t0"));

            CollectionAssert.AreEquivalent(new short[] { 9000, 9400 }, _saved.Select(q => q.QuestId).ToList());
        }

        [TestMethod]
        public async Task ADailyInstantCombatMissionIsReadWithItsTargetAndExperience()
        {
            await ParseAsync(Quest(9001, "9701\t0\t1\t4\t0", level: 3, data: "5\t-1", prize: "9717\t9723\t-1\t-1"));

            var quest = _saved.Single();
            Assert.AreEqual((short)9701, quest.NameItemVNum);
            Assert.AreEqual(FamilyQuestKind.Mission, quest.Kind);
            Assert.AreEqual(FamilyQuestObjective.InstantCombat, quest.Objective);
            Assert.AreEqual((byte)3, quest.FamilyLevelMinimum);
            Assert.AreEqual(5, quest.Target);
            Assert.AreEqual(11000, quest.Experience);
            Assert.IsTrue(quest.ResetsDaily);
            Assert.IsFalse(quest.OncePerMemberPerDay);
        }

        [TestMethod]
        public async Task ARaidMissionCountsEachMemberOncePerDay()
        {
            await ParseAsync(Quest(9007, "-1\t0\t1\t3\t0"));

            var quest = _saved.Single();
            Assert.AreEqual(FamilyQuestObjective.Raid, quest.Objective);
            Assert.IsTrue(quest.OncePerMemberPerDay);
        }

        [TestMethod]
        public async Task ALevelAchievementKeepsItsLinkAndParameter()
        {
            await ParseAsync(Quest(9401, "-1\t1\t0\t0\t0", data: "1\t5", link: 9400));

            var quest = _saved.Single();
            Assert.AreEqual(FamilyQuestKind.Achievement, quest.Kind);
            Assert.AreEqual(FamilyQuestObjective.FamilyLevel, quest.Objective);
            Assert.AreEqual(5, quest.Parameter);
            Assert.AreEqual((short)9400, quest.PreviousQuestId);
            Assert.IsFalse(quest.ResetsDaily);
        }

        [TestMethod]
        public async Task UnsetColumnsBecomeEmptyValues()
        {
            await ParseAsync(Quest(9402, "-1\t1\t9\t9\t0"));

            var quest = _saved.Single();
            Assert.AreEqual((short)0, quest.NameItemVNum);
            Assert.IsNull(quest.Objective);
            Assert.IsNull(quest.Parameter);
            Assert.IsNull(quest.PreviousQuestId);
            Assert.AreEqual((byte)0, quest.FamilyLevelMinimum);
            Assert.AreEqual(0, quest.Experience);
        }

        [TestMethod]
        [DataRow((short)8999, false)]
        [DataRow((short)9000, true)]
        [DataRow((short)9199, true)]
        [DataRow((short)9200, false)]
        [DataRow((short)9399, false)]
        [DataRow((short)9400, true)]
        [DataRow((short)9499, true)]
        [DataRow((short)9500, false)]
        public void FamilyQuestRangeBoundaries(short questId, bool isFamily) =>
            Assert.AreEqual(isFamily, FamilyQuestParser.IsFamilyQuest(questId));
    }
}
