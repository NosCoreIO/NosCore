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
using Serilog;
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
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<QuestDto, short>> _questDaoMock = null!;
        private Mock<IDao<QuestObjectiveDto, Guid>> _questObjectiveDaoMock = null!;
        private Mock<IDao<QuestRewardDto, short>> _questRewardDaoMock = null!;
        private Mock<IDao<QuestQuestRewardDto, Guid>> _questQuestRewardDaoMock = null!;
        private string _tempFolder = null!;
        private List<QuestDto> _savedQuests = null!;
        private List<QuestObjectiveDto> _savedObjectives = null!;
        private List<QuestQuestRewardDto> _savedQuestRewards = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
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
            bool isDaily = false,
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
                   $"VNUM\t{questId}\t{questType}\t{(autoFinish ? "1" : "0")}\t{(isDaily ? "-1" : "0")}\t{requiredQuestId?.ToString() ?? "-1"}\t{(isSecondary ? "1" : "-1")}\r\n" +
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

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
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

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(3, _savedQuests.Count);
        }

        [TestMethod]
        public async Task QuestParser_ParsesAutoFinishFlag()
        {
            var content = CreateQuestData(questId: 1, autoFinish: true);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.IsTrue(_savedQuests[0].AutoFinish);
        }

        [TestMethod]
        public async Task QuestParser_ParsesDailyFlag()
        {
            var content = CreateQuestData(questId: 1, isDaily: true);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.IsTrue(_savedQuests[0].IsDaily);
        }

        [TestMethod]
        public async Task QuestParser_ParsesNextQuestId()
        {
            var content = CreateQuestData(questId: 1, nextQuestId: 2);
            CreateTestFile(content);

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(1, _savedQuests.Count);
            Assert.AreEqual((short)2, _savedQuests[0].NextQuestId);
        }

        [TestMethod]
        public async Task QuestParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new QuestParser(_questDaoMock.Object, _questObjectiveDaoMock.Object, _questRewardDaoMock.Object, _questQuestRewardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestsAsync(_tempFolder);

            Assert.AreEqual(0, _savedQuests.Count);
        }
    }
}
