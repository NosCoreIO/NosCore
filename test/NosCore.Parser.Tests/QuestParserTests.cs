//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class QuestParserTests
    {
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<QuestDto, short>> _questDaoMock;
        private Mock<IDao<QuestObjectiveDto, Guid>> _questObjectiveDaoMock;
        private Mock<IDao<QuestRewardDto, short>> _questRewardDaoMock;
        private Mock<IDao<QuestQuestRewardDto, Guid>> _questQuestRewardDaoMock;
        private string _tempFolder;
        private List<QuestDto> _savedQuests;
        private List<QuestObjectiveDto> _savedObjectives;
        private List<QuestQuestRewardDto> _savedQuestRewards;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _questDaoMock = new Mock<IDao<QuestDto, short>>();
            _questObjectiveDaoMock = new Mock<IDao<QuestObjectiveDto, Guid>>();
            _questRewardDaoMock = new Mock<IDao<QuestRewardDto, short>>();
            _questQuestRewardDaoMock = new Mock<IDao<QuestQuestRewardDto, Guid>>();
            _savedQuests = [];
            _savedObjectives = [];
            _savedQuestRewards = [];

            _questDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<QuestDto>>()))
                .Callback<IEnumerable<QuestDto>>(quests => _savedQuests.AddRange(quests))
                .ReturnsAsync(true);

            _questObjectiveDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<QuestObjectiveDto>>()))
                .Callback<IEnumerable<QuestObjectiveDto>>(objectives => _savedObjectives.AddRange(objectives))
                .ReturnsAsync(true);

            _questQuestRewardDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<QuestQuestRewardDto>>()))
                .Callback<IEnumerable<QuestQuestRewardDto>>(rewards => _savedQuestRewards.AddRange(rewards))
                .ReturnsAsync(true);

            _questRewardDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<QuestRewardDto>());

            _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }

        private void CreateTestFile(string content)
        {
            File.WriteAllText(Path.Combine(_tempFolder, "quest.dat"), content);
        }

        private static string CreateQuestData(
            short questId = 1,
            int questType = 1,
            bool autoFinish = false,
            short questLineId = -1,
            short? requiredQuestId = null,
            bool isSecondary = false,
            byte levelMin = 1,
            byte levelMax = 99,
            string title = "TestQuest",
            string desc = "TestDescription",
            short? targetX = null,
            short? targetY = null,
            short? targetMap = null,
            int? startDialogId = null,
            int? endDialogId = null,
            short? nextQuestId = null)
        {
            return $"BEGIN\r\n" +
                   $"VNUM\t{questId}\t{questType}\t{(autoFinish ? "1" : "0")}\t{questLineId}\t{requiredQuestId?.ToString() ?? "-1"}\t{(isSecondary ? "1" : "-1")}\r\n" +
                   $"LEVEL\t{levelMin}\t{levelMax}\r\n" +
                   $"TITLE\t{title}\r\n" +
                   $"DESC\t{desc}\r\n" +
                   $"TALK\t{startDialogId?.ToString() ?? "-1"}\t{endDialogId?.ToString() ?? "-1"}\t0\t0\r\n" +
                   $"TARGET\t{targetX?.ToString() ?? "-1"}\t{targetY?.ToString() ?? "-1"}\t{targetMap?.ToString() ?? "-1"}\r\n" +
                   $"DATA\t-1\t-1\t-1\t-1\r\n" +
                   $"PRIZE\t-1\t-1\t-1\t-1\r\n" +
                   $"LINK\t{nextQuestId?.ToString() ?? "-1"}\r\n" +
                   "END";
        }

        [TestMethod]
        public async Task QuestParser_ParsesSingleQuest()
        {
            var content = CreateQuestData(questId: 1, title: "FirstQuest", levelMin: 10, levelMax: 50);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.AreEqual(1, _savedQuests[0].QuestId);
            Assert.AreEqual("FirstQuest", _savedQuests[0].TitleI18NKey);
            Assert.AreEqual(10, _savedQuests[0].LevelMin);
            Assert.AreEqual(50, _savedQuests[0].LevelMax);
        }

        [TestMethod]
        public async Task QuestParser_ParsesMultipleQuests()
        {
            var content = CreateQuestData(questId: 1, title: "Quest1") + "\n#=======\n" +
                          CreateQuestData(questId: 2, title: "Quest2") + "\n#=======\n" +
                          CreateQuestData(questId: 3, title: "Quest3");
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(3, _savedQuests.Count);
        }

        [TestMethod]
        public async Task QuestParser_ParsesAutoFinishFlag()
        {
            var content = CreateQuestData(questId: 1, autoFinish: true);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.IsTrue(_savedQuests[0].AutoFinish);
        }

        [TestMethod]
        public async Task QuestParser_ADailyQuestIsDaily()
        {
            CreateTestFile(CreateQuestData(questId: 7516, questLineId: 1));

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.IsTrue(_savedQuests.Single().IsDaily);
        }

        [TestMethod]
        public async Task QuestParser_AQuestOutsideAnyQuestLineIsNotDaily()
        {
            CreateTestFile(CreateQuestData(questId: 100, questLineId: -1));

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.IsFalse(_savedQuests.Single().IsDaily);
        }

        [TestMethod]
        public async Task QuestParser_ParsesNextQuestId()
        {
            var content = CreateQuestData(questId: 1, nextQuestId: 2);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.AreEqual((short)2, _savedQuests[0].NextQuestId);
        }

        [TestMethod]
        public async Task QuestParser_AQuestBelongsToTheLineOfTheFirstQuestOfItsChain()
        {
            CreateTestFile(CreateQuestData(questId: 2100, nextQuestId: 2101) + "\n#=======\n" +
                           CreateQuestData(questId: 2101, nextQuestId: 2102) + "\n#=======\n" +
                           CreateQuestData(questId: 2102) + "\n#=======\n" +
                           CreateQuestData(questId: 5905));

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            CollectionAssert.AreEqual(new short[] { 2100, 2100, 2100, 5905 },
                _savedQuests.OrderBy(q => q.QuestId).Select(q => q.QuestLineId).ToArray());
        }

        [TestMethod]
        public async Task QuestParser_QuestsLinkingToEachOtherStartTheirLineAtTheLowerId()
        {
            CreateTestFile(CreateQuestData(questId: 5030, nextQuestId: 5006) + "\n#=======\n" +
                           CreateQuestData(questId: 5006, nextQuestId: 5030));

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.IsTrue(_savedQuests.All(q => q.QuestLineId == 5006));
        }

        [TestMethod]
        public async Task QuestParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(0, _savedQuests.Count);
        }

        [TestMethod]
        public async Task QuestParser_AReimportKeepsTheStoredObjectiveAndPrizeIds()
        {
            CreateTestFile(CreateQuestData(questId: 7).Replace("DATA\t-1\t-1\t-1\t-1", "DATA\t25\t5\t5\t-1")
                .Replace("PRIZE\t-1\t-1\t-1\t-1", "PRIZE\t4\t-1\t-1\t-1"));
            _questRewardDaoMock.Setup(x => x.LoadAll()).Returns(new List<QuestRewardDto> { new() { QuestRewardId = 4 } });
            var objectiveId = Guid.NewGuid();
            var linkId = Guid.NewGuid();
            _questObjectiveDaoMock.Setup(x => x.LoadAll()).Returns(new List<QuestObjectiveDto>
            {
                new() { QuestObjectiveId = objectiveId, QuestId = 7, FirstData = 25, SecondData = 5, ThirdData = 5 },
            });
            _questQuestRewardDaoMock.Setup(x => x.LoadAll()).Returns(new List<QuestQuestRewardDto>
            {
                new() { Id = linkId, QuestId = 7, QuestRewardId = 4 },
            });

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(objectiveId, _savedObjectives.Single().QuestObjectiveId);
            Assert.AreEqual(linkId, _savedQuestRewards.Single().Id);
        }
    }
}
