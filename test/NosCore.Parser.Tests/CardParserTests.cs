//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
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
    public class CardParserTests
    {
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<CardDto, short>> _cardDaoMock;
        private Mock<IDao<BCardDto, int>> _bCardDaoMock;
        private string _tempFolder;
        private List<CardDto> _savedCards;
        private List<BCardDto> _savedBCards;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _cardDaoMock = new Mock<IDao<CardDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, int>>();
            _savedCards = [];
            _savedBCards = [];

            _cardDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<CardDto>>()))
                .Callback<IEnumerable<CardDto>>(cards => _savedCards.AddRange(cards))
                .ReturnsAsync(true);

            _bCardDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<BCardDto>>()))
                .Callback<IEnumerable<BCardDto>>(cards => _savedBCards.AddRange(cards))
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

        private void CreateTestFile(string content)
        {
            File.WriteAllText(Path.Combine(_tempFolder, "Card.dat"), content);
        }

        private static string CreateCardData(
            short cardId = 1,
            string name = "TestCard",
            byte level = 1,
            int effectId = 0,
            byte buffType = 0,
            int duration = 0,
            int delay = 0,
            short timeoutBuff = 0,
            byte timeoutBuffChance = 0,
            short group = 0)
        {
            return $"\tVNUM\t{cardId}\r\n" +
                   $"\tNAME\t{name}\r\n" +
                   $"\tGROUP\t{group}\t{level}\t0\r\n" +
                   $"\tSTYLE\t0\t{buffType}\t0\t0\r\n" +
                   $"\tEFFECT\t{effectId}\t0\r\n" +
                   $"\tTIME\t{duration}\t{delay}\r\n" +
                   "\t1ST\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
                   "\t2ST\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
                   $"\tLAST\t{timeoutBuff}\t{timeoutBuffChance}\r\n" +
                   "\tDESC\tTest Description\r\n" +
                   "END";
        }

        [TestMethod]
        public async Task CardParser_ReadsTheBuffFamilyAndWhetherTheBuffIsBad()
        {
            CreateTestFile(CreateCardData(cardId: 339, buffType: 2, group: 404));

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(BuffType.Bad, _savedCards.Single().BuffType);
            Assert.AreEqual(404, _savedCards.Single().GroupId);
        }

        [TestMethod]
        public async Task CardParser_MarksTheCardsTheServerAppliesOnItsOwn()
        {
            CreateTestFile(CreateCardData(cardId: 319) + "\r\n" + CreateCardData(cardId: 169));

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(CardTrigger.SpouseInGroup, _savedCards.Single(c => c.CardId == 319).Trigger);
            Assert.AreEqual(CardTrigger.ArenaRevival, _savedCards.Single(c => c.CardId == 169).Trigger);
        }

        [TestMethod]
        public async Task CardParser_HeatResistanceLastsUpToTwoHours()
        {
            CreateTestFile(CreateCardData(cardId: 340));

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(12000, _savedCards.Single().Duration);
        }

        [TestMethod]
        public async Task CardParser_TheFairyBoosterRaisesTheFairyElement()
        {
            CreateTestFile(CreateCardData(cardId: 131));

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            var card = _savedBCards.Single(b => b.CardId == 131);
            Assert.AreEqual(BCardEffect.Type96IncreaseFairyElement, card.Effect());
            Assert.AreEqual(30, card.FirstData);
        }

        [TestMethod]
        public async Task CardParser_ParsesSingleCard()
        {
            var content = CreateCardData(cardId: 1, name: "Buff1", level: 5, duration: 100);
            CreateTestFile(content);

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(1, _savedCards.Count);
            Assert.AreEqual(1, _savedCards[0].CardId);
            Assert.AreEqual("Buff1", _savedCards[0].NameI18NKey);
            Assert.AreEqual(5, _savedCards[0].Level);
            Assert.AreEqual(100, _savedCards[0].Duration);
        }

        [TestMethod]
        public async Task CardParser_ParsesMultipleCards()
        {
            var content = CreateCardData(cardId: 1, name: "Buff1") + "\n#========================================================\n" +
                          CreateCardData(cardId: 2, name: "Buff2") + "\n#========================================================\n" +
                          CreateCardData(cardId: 3, name: "Buff3");
            CreateTestFile(content);

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(3, _savedCards.Count);
        }

        [TestMethod]
        public async Task CardParser_DeduplicatesByCardId()
        {
            var content = CreateCardData(cardId: 1, name: "Buff1") + "\n#========================================================\n" +
                          CreateCardData(cardId: 1, name: "Buff1Duplicate");
            CreateTestFile(content);

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(1, _savedCards.Count);
        }

        [TestMethod]
        public async Task CardParser_ParsesDelayField()
        {
            var content = CreateCardData(cardId: 1, delay: 500);
            CreateTestFile(content);

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(1, _savedCards.Count);
            Assert.AreEqual(500, _savedCards[0].Delay);
        }

        [TestMethod]
        public async Task CardParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new CardParser(_cardDaoMock.Object, _bCardDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertCardsAsync(_tempFolder);

            Assert.AreEqual(0, _savedCards.Count);
        }

        [TestMethod]
        public async Task ParsingTheSameCardsTwiceKeepsOneCopyOfEachBCard()
        {
            CreateTestFile(CreateCardData(cardId: 121).Replace(
                "\t1ST\t0\t0\t0\t0\t0\t0",
                "\t1ST\t44\t1\t0\t0\t400\t0"));
            var options = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            NosCoreContext Context() => new NosCoreContext(options);
            var cardDao = new Dao<Database.Entities.Card, CardDto, short>(NullLogger<Dao<Database.Entities.Card, CardDto, short>>.Instance, Context);
            var bCardDao = new Dao<Database.Entities.BCard, BCardDto, int>(NullLogger<Dao<Database.Entities.BCard, BCardDto, int>>.Instance, Context);
            var parser = new CardParser(cardDao, bCardDao, NullLoggerFactory.Instance, _logLanguageMock.Object);

            await parser.InsertCardsAsync(_tempFolder);
            await parser.InsertCardsAsync(_tempFolder);

            var bCard = bCardDao.LoadAll().Single();
            Assert.AreEqual((short)121, bCard.CardId);
            Assert.AreEqual(100, bCard.FirstData);
        }
    }
}
