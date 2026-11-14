//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.MateHpService;
using NosCore.Algorithm.MateMpService;
using NosCore.Algorithm.MonsterStatService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.Map;
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
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<SkillDto, short>> _skillDaoMock;
        private Mock<IDao<BCardDto, int>> _bCardDaoMock;
        private Mock<IDao<DropDto, short>> _dropDaoMock;
        private Mock<IDao<NpcMonsterSkillDto, long>> _npcMonsterSkillDaoMock;
        private Mock<IDao<NpcMonsterDto, short>> _npcMonsterDaoMock;
        private static readonly MonsterStatService MonsterStats = new(new MateHpService(), new MateMpService());
        private string _tempFolder;
        private List<NpcMonsterDto> _savedMonsters;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _skillDaoMock = new Mock<IDao<SkillDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, int>>();
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
            int hp = 0, int mp = 0, int xp = 0, int jxp = 0, byte armorLvl = 1, byte weaponLvl = 1, byte weaponType = 0,
            string weaponModifiers = "0\t0\t0\t0\t0", string armorModifiers = "0\t0\t0\t0", byte attackClass = 0,
            long etc = 0, string winfo = "0\t0\t0", string ainfo = "0\t0", string preatt = "0\t0\t0\t0\t0\t0",
            string setting = "0\t0\t0\t0\t0\t0", string? mode = null)
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
                $"\tPREATT\t{preatt}\r\n" +
                $"\tSETTING\t{setting}\r\n" +
                $"\tETC\t{etc}\t0\t0\t0\t0\t0\t0\t0\r\n" +
                $"\tPETINFO\t0\t0\t0\t0\r\n" +
                $"\tEFF\t0\t0\t0\r\n" +
                $"\tZSKILL\t{attackClass}\t0\t0\t0\t0\t0\r\n" +
                $"\tWINFO\t{winfo}\r\n" +
                $"\tWEAPON\t{weaponLvl}\t{weaponType}\t{weaponModifiers}\r\n" +
                $"\tAINFO\t{ainfo}\r\n" +
                $"\tARMOR\t{armorLvl}\t{armorModifiers}\r\n" +
                $"\tSKILL\t{zeros15}\r\n" +
                $"\tPARTNER\t{zeros20}\r\n" +
                $"\tBASIC\t{zeros50}\r\n" +
                $"\tCARD\t{zeros20}\r\n" +
                $"\tMODE\t{mode ?? zeros32}\r\n" +
                $"\tITEM\t{zeros60}\r\n" +
                "#========================================================\r\n";
        }

        [TestMethod]
        public async Task NpcMonsterParser_ParsesBasicVnumAndName()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(1, "testmob", 1));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual(1, _savedMonsters[0].NpcMonsterVNum);
            Assert.AreEqual("testmob", _savedMonsters[0].NameI18NKey);
        }

        [TestMethod]
        public async Task NpcMonsterParser_AStationaryMonsterTakesItsUpgradesFromTheUpgradeColumns()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(396, "zts396e", 10,
                etc: 1, winfo: "1\t0\t5", ainfo: "0\t3"));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            var monster = _savedMonsters.Single();
            Assert.AreEqual(5, monster.AttackUpgrade);
            Assert.AreEqual(3, monster.DefenceUpgrade);
        }

        [TestMethod]
        public async Task NpcMonsterParser_EtcBit30MarksAMapBoss()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"),
                BuildMonster(284, "zts284e", 55, etc: 1073744960) + BuildMonster(27, "zts27e", 14, etc: 1028));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(MonsterType.Boss, _savedMonsters.Single(m => m.NpcMonsterVNum == 284).MonsterType);
            Assert.AreEqual(MonsterType.Unknown, _savedMonsters.Single(m => m.NpcMonsterVNum == 27).MonsterType);
        }

        [TestMethod]
        public async Task NpcMonsterParser_DeduplicatesByVNum()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"),
                BuildMonster(5, "first", 1) + BuildMonster(5, "duplicate", 1));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
        }

        [TestMethod]
        public async Task NpcMonsterParser_LevelFieldParsedCorrectly()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(10, "leveltest", 42));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual(42, _savedMonsters[0].Level);
        }

        [TestMethod]
        public async Task NpcMonsterParser_StatsMatchThePublishedOnesForARace2MonsterInHigherLevelArmour()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(145, "zts145e", 41, race: 2,
                armorLvl: 44, armorModifiers: "0\t-20\t0\t0", weaponLvl: 44, weaponType: 1, weaponModifiers: "20\t25\t0\t0\t50"));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            var monster = _savedMonsters.Single();
            Assert.AreEqual(3493, monster.MaxHp);
            Assert.AreEqual(580, monster.MaxMp);
            Assert.AreEqual(136, monster.CloseDefence);
            Assert.AreEqual(120, monster.DistanceDefence);
            Assert.AreEqual(120, monster.MagicDefence);
            Assert.AreEqual(246, monster.DefenceDodge);
            Assert.AreEqual(247, monster.DamageMinimum);
            Assert.AreEqual(330, monster.DamageMaximum);
            Assert.AreEqual(242, monster.Concentrate);
            Assert.AreEqual(4, monster.CriticalChance);
            Assert.AreEqual(120, monster.CriticalRate);
            Assert.AreEqual(2870, monster.Xp);
            Assert.AreEqual(105, monster.JobXp);
        }

        [TestMethod]
        public async Task NpcMonsterParser_RangedMonsterGetsItsDamageAndANegativeExpLowersTheAward()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(2666, "zts2666e", 81, race: 5,
                xp: -4670, jxp: -55, armorLvl: 81, weaponLvl: 81, weaponType: 8, weaponModifiers: "0\t50\t0\t5\t50", attackClass: 1));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            var monster = _savedMonsters.Single();
            Assert.AreEqual(7970, monster.MaxHp);
            Assert.AreEqual(1494, monster.MaxMp);
            Assert.AreEqual(347, monster.DamageMinimum);
            Assert.AreEqual(533, monster.DamageMaximum);
            Assert.AreEqual(482, monster.Concentrate);
            Assert.AreEqual(9, monster.CriticalChance);
            Assert.AreEqual(120, monster.CriticalRate);
            Assert.AreEqual(213, monster.CloseDefence);
            Assert.AreEqual(415, monster.DefenceDodge);
            Assert.AreEqual(1000, monster.Xp);
            Assert.AreEqual(50, monster.JobXp);
        }

        [TestMethod]
        public async Task NpcMonsterParser_ANegativeExpLargerThanTheAwardGivesNothing()
        {
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(2667, "zts2667e", 10, race: 0,
                xp: -700, jxp: -200, armorLvl: 10, weaponLvl: 10));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            var monster = _savedMonsters.Single();
            Assert.AreEqual(0, monster.Xp);
            Assert.AreEqual(0, monster.JobXp);
        }

        [TestMethod]
        public async Task NpcMonsterParser_ImportsABossWhoseSizeAndRangesOutgrowAByte()
        {
            var mode = string.Join("\t", Enumerable.Repeat("0", 29).Append("462").Append("0").Append("0"));
            File.WriteAllText(Path.Combine(_tempFolder, "monster.dat"), BuildMonster(563, "zts649e", 80, race: 3,
                weaponLvl: 80, weaponType: 25, weaponModifiers: "420\t500\t100\t-40\t100", preatt: "1\t0\t1000\t0\t36000",
                setting: "563\t0\t0\t250\t2\t0", mode: mode));
            var parser = new NpcMonsterParser(_skillDaoMock.Object, _bCardDaoMock.Object, _dropDaoMock.Object,
                _npcMonsterSkillDaoMock.Object, _npcMonsterDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object, MonsterStats);
            await parser.InsertNpcMonstersAsync(_tempFolder);

            var monster = _savedMonsters.Single();
            Assert.AreEqual(250, monster.SpriteSize);
            Assert.AreEqual(1000, monster.NoticeRange);
            Assert.AreEqual(462, monster.CellMinRange);
            Assert.AreEqual(0, monster.CriticalChance);
        }
    }
}
