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
    public class NpcMonsterParserTests
    {
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<SkillDto, short>> _skillDaoMock = null!;
        private Mock<IDao<BCardDto, short>> _bCardDaoMock = null!;
        private Mock<IDao<DropDto, short>> _dropDaoMock = null!;
        private Mock<IDao<NpcMonsterSkillDto, long>> _npcMonsterSkillDaoMock = null!;
        private Mock<IDao<NpcMonsterDto, short>> _npcMonsterDaoMock = null!;
        private string _tempFolder = null!;
        private List<NpcMonsterDto> _savedMonsters = null!;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _skillDaoMock = new Mock<IDao<SkillDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, short>>();
            _dropDaoMock = new Mock<IDao<DropDto, short>>();
            _npcMonsterSkillDaoMock = new Mock<IDao<NpcMonsterSkillDto, long>>();
            _npcMonsterDaoMock = new Mock<IDao<NpcMonsterDto, short>>();
            _savedMonsters = [];

            _skillDaoMock.Setup(x => x.LoadAll()).Returns(new List<SkillDto>());
            _dropDaoMock.Setup(x => x.LoadAll()).Returns(new List<DropDto>());

            _npcMonsterDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcMonsterDto>>()))
                .Callback<IEnumerable<NpcMonsterDto>>(m => _savedMonsters.AddRange(m))
                .ReturnsAsync(true);
            _bCardDaoMock.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<BCardDto>>())).ReturnsAsync(true);
            _dropDaoMock.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<DropDto>>())).ReturnsAsync(true);
            _npcMonsterSkillDaoMock.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<NpcMonsterSkillDto>>())).ReturnsAsync(true);

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

        private static string BuildMonster(short vnum, string name, byte level, byte race = 0, byte raceType = 0,
            int hp = 0, int mp = 0, int xp = 0, int jxp = 0, byte armorLvl = 1, byte weaponLvl = 1, byte weaponType = 0)
        {
            var zeros50 = string.Join("\t", Enumerable.Repeat("0", 50));
            var zeros20 = string.Join("\t", Enumerable.Repeat("0", 20));
            var zeros60 = string.Join("\t", Enumerable.Repeat("0", 60));
            var zeros15 = string.Join("\t", Enumerable.Repeat("0", 15));
            var zeros32 = string.Join("\t", Enumerable.Repeat("0", 32));
            return
                $"\tVNUM\t{vnum}\r\n" +
                $"\tNAME\t{name}\r\n" +
                $"\tLEVEL\t{level}\r\n" +
                $"\tRACE\t{race}\t{raceType}\r\n" +
                $"\tATTRIB\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tHP/MP\t{hp}\t{mp}\r\n" +
                $"\tEXP\t{xp}\t{jxp}\r\n" +
                $"\tPREATT\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tSETTING\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tETC\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tPETINFO\t0\t0\t0\t0\r\n" +
                $"\tEFF\t0\t0\t0\r\n" +
                $"\tZSKILL\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tWINFO\t0\t0\t0\r\n" +
                $"\tWEAPON\t{weaponLvl}\t{weaponType}\t0\t0\t0\t0\t0\r\n" +
                $"\tAINFO\t0\t0\r\n" +
                $"\tARMOR\t{armorLvl}\t0\t0\t0\t0\r\n" +
                $"\tSKILL\t{zeros15}\r\n" +
                $"\tPARTNER\t{zeros20}\r\n" +
                $"\tBASIC\t{zeros50}\r\n" +
                $"\tCARD\t{zeros20}\r\n" +
                $"\tMODE\t{zeros32}\r\n" +
                $"\tITEM\t{zeros60}\r\n" +
                "#========================================================\r\n";
        }

        [TestMethod]
        public async Task NpcMonsterParser_ParsesBasicVnumAndName()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(1, "testmob", 1));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual(1, _savedMonsters[0].NpcMonsterVNum);
            Assert.AreEqual("testmob", _savedMonsters[0].NameI18NKey);
        }

        [TestMethod]
        public async Task NpcMonsterParser_DeduplicatesByVNum()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"),
                BuildMonster(5, "first", 1) + BuildMonster(5, "duplicate", 1));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
        }

        [TestMethod]
        public async Task NpcMonsterParser_LevelFieldParsedCorrectly()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(10, "leveltest", 42));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual(42, _savedMonsters[0].Level);
        }

        [TestMethod]
        public async Task NpcMonsterParser_ArmorDefenceFieldsDerivedFromArmorLevel()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(20, "armortest", 1, armorLvl: 5));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual((5 - 1) * 2 + 18, _savedMonsters[0].CloseDefence);
            Assert.AreEqual((5 - 1) * 3 + 17, _savedMonsters[0].DistanceDefence);
            Assert.AreEqual((5 - 1) * 2 + 13, _savedMonsters[0].MagicDefence);
            Assert.AreEqual((5 - 1) * 5 + 31, _savedMonsters[0].DefenceDodge);
        }
    }
}
