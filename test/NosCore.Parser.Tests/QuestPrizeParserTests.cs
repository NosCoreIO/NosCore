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
using NosCore.Packets.Enumerations;
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
    public class QuestPrizeParserTests
    {
        private Mock<IDao<QuestRewardDto, short>> _daoMock;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private List<QuestRewardDto> _saved;
        private string _tempFolder;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _daoMock = new Mock<IDao<QuestRewardDto, short>>();
            _saved = [];
            _daoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<QuestRewardDto>>()))
                .Callback<IEnumerable<QuestRewardDto>>(r => _saved.AddRange(r))
                .ReturnsAsync(true);

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

        private void WriteFile(string content) =>
            File.WriteAllText(Path.Combine(_tempFolder, "qstprize.dat"), content);

        private static string Entry(short id, short rewardType, string dataLine) =>
            $"VNUM\t{id}\t{rewardType}\r\nDATA\t{dataLine}\r\nEND\r\n";

        [TestMethod]
        public async Task QuestPrizeParser_GoldRewardParsesAmountFromFirstDataField()
        {
            WriteFile(Entry(100, (short)QuestRewardType.Gold, "500\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(100, _saved[0].QuestRewardId);
            Assert.AreEqual(QuestRewardType.Gold, _saved[0].RewardType);
            Assert.AreEqual(0, _saved[0].Data);
            Assert.AreEqual(500, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ExpRewardParsesAmountAndData()
        {
            WriteFile(Entry(200, (short)QuestRewardType.Experience, "10\t50\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(QuestRewardType.Experience, _saved[0].RewardType);
            Assert.AreEqual(50, _saved[0].Data);
            Assert.AreEqual(10, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ExpRewardWithMinusOneDataYieldsZero()
        {
            WriteFile(Entry(201, (short)QuestRewardType.Experience, "1000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(0, _saved[0].Data);
            Assert.AreEqual(1000, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_WearItemRewardStoresVnumAndAmountOne()
        {
            WriteFile(Entry(300, (short)QuestRewardType.WearItem, "2000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(2000, _saved[0].Data);
            Assert.AreEqual(1, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_EtcMainItemUsesDataFieldFiveForAmount()
        {
            WriteFile(Entry(400, (short)QuestRewardType.EtcMainItem, "1012\t-1\t-1\t-1\t10"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(1012, _saved[0].Data);
            Assert.AreEqual(10, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_EtcMainItemMinusOneAmountFallsBackToOne()
        {
            WriteFile(Entry(401, (short)QuestRewardType.EtcMainItem, "1013\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(1013, _saved[0].Data);
            Assert.AreEqual(1, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ParsesMultipleEntries()
        {
            WriteFile(
                Entry(1, (short)QuestRewardType.Gold, "100\t-1\t-1\t-1\t-1") +
                Entry(2, (short)QuestRewardType.Experience, "500\t1000\t-1\t-1\t-1") +
                Entry(3, (short)QuestRewardType.WearItem, "2000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(3, _saved.Count);
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 1 && r.Amount == 100));
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 2 && r.Amount == 500 && r.Data == 1000));
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 3 && r.Data == 2000));
        }

        [TestMethod]
        public async Task QuestPrizeParser_ExpRewardKeepsANegativeLevel()
        {
            WriteFile(Entry(202, (short)QuestRewardType.Experience, "4\t-80\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(-80, _saved[0].Data);
            Assert.AreEqual(4, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ItemChoiceKeepsEveryVnum()
        {
            WriteFile(Entry(11, (short)QuestRewardType.EtcMainItem, "1002\t1005\t1008\t-1\t3"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1002, _saved[0].Data);
            Assert.AreEqual(1005, _saved[0].SecondData);
            Assert.AreEqual(1008, _saved[0].ThirdData);
            Assert.IsNull(_saved[0].FourthData);
            Assert.AreEqual(3, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_LevelAndJobLevelStoresBothLevels()
        {
            WriteFile(Entry(1349, (short)QuestRewardType.LevelAndJobLevel, "20\t5\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(20, _saved[0].Data);
            Assert.AreEqual(5, _saved[0].SecondData);
        }
    }
}
