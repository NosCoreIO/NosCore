//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
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
    public class ItemParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<ItemDto, short>> _itemDaoMock = null!;
        private Mock<IDao<BCardDto, short>> _bCardDaoMock = null!;
        private string _tempFolder = null!;
        private List<ItemDto> _savedItems = null!;
        private List<BCardDto> _savedBCards = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _itemDaoMock = new Mock<IDao<ItemDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, short>>();
            _savedItems = [];
            _savedBCards = [];

            _itemDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ItemDto>>()))
                .Callback<IEnumerable<ItemDto>>(items => _savedItems.AddRange(items))
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
            File.WriteAllText(Path.Combine(_tempFolder, "Item.dat"), content);
        }

        private static string CreateItemData(
            short vnum = 1,
            long price = 100,
            string name = "TestItem",
            int indexType = 0,
            int indexSubType = 0,
            int indexItemType = 0,
            int equipmentSlot = -1,
            int morph = 0,
            int typeValue = 0,
            int typeClass = 0,
            string flags = "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0",
            string data = "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0",
            string buff = "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0")
        {
            // Format: lines have a leading tab so keyword is at index 1 after splitting
            return $"\tVNUM\t{vnum}\t{price}\r\n" +
                   $"\tNAME\t{name}\r\n" +
                   $"\tINDEX\t{indexType}\t{indexSubType}\t{indexItemType}\t{equipmentSlot}\t{morph}\t0\t0\r\n" +
                   $"\tTYPE\t{typeValue}\t{typeClass}\r\n" +
                   $"\tFLAG\t{flags}\r\n" +
                   $"\tDATA\t{data}\r\n" +
                   $"\tBUFF\t{buff}\r\n" +
                   "\tLINEDESC\t0\r\n" +
                   "Test Description\r\n" +
                   "END";
        }

        [TestMethod]
        public async Task ItemParser_ParsesSingleItem()
        {
            var content = CreateItemData(vnum: 1, price: 500, name: "Sword");
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.AreEqual(1, _savedItems[0].VNum);
            Assert.AreEqual(500, _savedItems[0].Price);
            Assert.AreEqual("Sword", _savedItems[0].NameI18NKey);
        }

        [TestMethod]
        public async Task ItemParser_ParsesMultipleItems()
        {
            var content = CreateItemData(vnum: 1, name: "Item1") + "\n#========================================================\n" +
                          CreateItemData(vnum: 2, name: "Item2") + "\n#========================================================\n" +
                          CreateItemData(vnum: 3, name: "Item3");
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(3, _savedItems.Count);
        }

        [TestMethod]
        public async Task ItemParser_DeduplicatesByVNum()
        {
            var content = CreateItemData(vnum: 1, name: "Item1") + "\n#========================================================\n" +
                          CreateItemData(vnum: 1, name: "Item1Duplicate");
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
        }

        [TestMethod]
        public async Task ItemParser_ParsesWeaponType()
        {
            // Equipment type is determined by INDEX field
            // indexType=4 (Equipment), indexItemType=0 (Weapon subtype), equipmentSlot=0 (MainWeapon)
            var content = CreateItemData(
                vnum: 1,
                indexType: 4,
                indexSubType: 0,
                indexItemType: 0,
                equipmentSlot: 0,
                typeClass: 1
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.AreEqual(ItemType.Weapon, _savedItems[0].ItemType);
            Assert.AreEqual(EquipmentType.MainWeapon, _savedItems[0].EquipmentSlot);
        }

        [TestMethod]
        public async Task ItemParser_ParsesArmorType()
        {
            // INDEX[0][2]=4 (Equipment pocket), INDEX[0][3]=1 (Armor suffix) → ItemType = "01" = Armor
            // Equipment slot at INDEX[0][5] = 1 (Armor)
            var content = CreateItemData(
                vnum: 100,
                indexType: 4,
                indexSubType: 1,  // This is the itemType suffix (1 = Armor when combined with Equipment)
                indexItemType: 0,
                equipmentSlot: 1,
                typeClass: 1
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.AreEqual(ItemType.Armor, _savedItems[0].ItemType);
            Assert.AreEqual(EquipmentType.Armor, _savedItems[0].EquipmentSlot);
        }

        [TestMethod]
        public async Task ItemParser_ParsesNonDroppableFlag()
        {
            // IsDroppable = FLAG[0][6] == "0", so FLAG[0][6] = "1" means NOT droppable
            // FLAG[0][6] is position 4 in the flags string (6 - 2 for "" and "FLAG")
            var content = CreateItemData(
                vnum: 1,
                flags: "0\t0\t0\t0\t1\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.IsFalse(_savedItems[0].IsDroppable);
        }

        [TestMethod]
        public async Task ItemParser_ParsesTradableItem()
        {
            var content = CreateItemData(
                vnum: 1,
                flags: "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.IsTrue(_savedItems[0].IsDroppable);
            Assert.IsTrue(_savedItems[0].IsTradable);
            Assert.IsTrue(_savedItems[0].IsSoldable);
        }

        [TestMethod]
        public async Task ItemParser_ParsesBCards()
        {
            var content = CreateItemData(
                vnum: 1,
                buff: "0\t1\t10\t100\t50\t1\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.IsTrue(_savedBCards.Count > 0);
        }

        [TestMethod]
        public async Task ItemParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(0, _savedItems.Count);
        }

        [TestMethod]
        public async Task ItemParser_ParsesHeroicFlag()
        {
            // IsHeroic is at FLAG[0][22], which is the 20th position in flags string (22 - 2 for "" and "FLAG")
            var content = CreateItemData(
                vnum: 1,
                flags: "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t1\t0\t0\t0"
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.IsTrue(_savedItems[0].IsHeroic);
        }

        [TestMethod]
        public async Task ItemParser_ParsesConsumableItem()
        {
            var content = CreateItemData(
                vnum: 2000,
                indexType: 9,
                indexSubType: 0,
                indexItemType: 0,
                equipmentSlot: -1,
                data: "0\t0\t100\t0\t50\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"
            );
            CreateTestFile(content);

            var parser = new ItemParser(_itemDaoMock.Object, _bCardDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ParseAsync(_tempFolder);

            Assert.AreEqual(1, _savedItems.Count);
            Assert.AreEqual(NoscorePocketType.Main, _savedItems[0].Type);
        }
    }
}
