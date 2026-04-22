//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Quest;
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
    public class QuestPrizeParserTests
    {
        private Mock<IDao<QuestRewardDto, short>> _daoMock = null!;
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private List<QuestRewardDto> _saved = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
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

        private static string Entry(short id, byte rewardType, string dataLine) =>
            $"VNUM\t{id}\t{rewardType}\r\nDATA\t{dataLine}\r\nEND\r\n";

        [TestMethod]
        public async Task QuestPrizeParser_GoldRewardParsesAmountFromFirstDataField()
        {
            WriteFile(Entry(100, (byte)QuestRewardType.Gold, "500\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(100, _saved[0].QuestRewardId);
            Assert.AreEqual((byte)QuestRewardType.Gold, _saved[0].RewardType);
            Assert.AreEqual(0, _saved[0].Data);
            Assert.AreEqual(500, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ExpRewardParsesAmountAndData()
        {
            WriteFile(Entry(200, (byte)QuestRewardType.Exp, "1000\t5000\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual((byte)QuestRewardType.Exp, _saved[0].RewardType);
            Assert.AreEqual(5000, _saved[0].Data);
            Assert.AreEqual(1000, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ExpRewardWithMinusOneDataYieldsZero()
        {
            WriteFile(Entry(201, (byte)QuestRewardType.Exp, "1000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(0, _saved[0].Data);
            Assert.AreEqual(1000, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_WearItemRewardStoresVnumAndAmountOne()
        {
            WriteFile(Entry(300, (byte)QuestRewardType.WearItem, "2000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(2000, _saved[0].Data);
            Assert.AreEqual(1, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_EtcMainItemUsesDataFieldFiveForAmount()
        {
            WriteFile(Entry(400, (byte)QuestRewardType.EtcMainItem, "1012\t-1\t-1\t-1\t10"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(1012, _saved[0].Data);
            Assert.AreEqual(10, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_EtcMainItemMinusOneAmountFallsBackToOne()
        {
            WriteFile(Entry(401, (byte)QuestRewardType.EtcMainItem, "1013\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(1013, _saved[0].Data);
            Assert.AreEqual(1, _saved[0].Amount);
        }

        [TestMethod]
        public async Task QuestPrizeParser_ParsesMultipleEntries()
        {
            WriteFile(
                Entry(1, (byte)QuestRewardType.Gold, "100\t-1\t-1\t-1\t-1") +
                Entry(2, (byte)QuestRewardType.Exp, "500\t1000\t-1\t-1\t-1") +
                Entry(3, (byte)QuestRewardType.WearItem, "2000\t-1\t-1\t-1\t-1"));
            var parser = new QuestPrizeParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportQuestPrizesAsync(_tempFolder);

            Assert.AreEqual(3, _saved.Count);
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 1 && r.Amount == 100));
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 2 && r.Amount == 500 && r.Data == 1000));
            Assert.IsTrue(_saved.Any(r => r.QuestRewardId == 3 && r.Data == 2000));
        }
    }
}
