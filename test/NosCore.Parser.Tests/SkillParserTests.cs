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
    public class SkillParserTests
    {
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<SkillDto, short>> _skillDaoMock = null!;
        private Mock<IDao<BCardDto, int>> _bCardDaoMock = null!;
        private Mock<IDao<ComboDto, int>> _comboDaoMock = null!;
        private string _tempFolder = null!;
        private List<SkillDto> _savedSkills = null!;
        private List<BCardDto> _savedBCards = null!;
        private List<ComboDto> _savedCombos = null!;

        [TestInitialize]
        public void Setup()
        {
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _skillDaoMock = new Mock<IDao<SkillDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, int>>();
            _comboDaoMock = new Mock<IDao<ComboDto, int>>();
            _savedSkills = [];
            _savedBCards = [];
            _savedCombos = [];

            _skillDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<SkillDto>>()))
                .Callback<IEnumerable<SkillDto>>(skills => _savedSkills.AddRange(skills))
                .ReturnsAsync(true);

            _bCardDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<BCardDto>>()))
                .Callback<IEnumerable<BCardDto>>(cards => _savedBCards.AddRange(cards))
                .ReturnsAsync(true);

            _comboDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ComboDto>>()))
                .Callback<IEnumerable<ComboDto>>(combos => _savedCombos.AddRange(combos))
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
            File.WriteAllText(Path.Combine(_tempFolder, "Skill.dat"), content);
        }

        private static string CreateSkillData(
            short skillVNum = 1,
            string name = "TestSkill",
            byte skillType = 0,
            short castId = 0,
            byte classType = 0,
            byte type = 0,
            byte element = 0,
            byte cpCost = 0,
            int price = 0,
            short castEffect = 0,
            short castAnimation = 0,
            short effect = 0,
            short attackAnimation = 0,
            byte targetType = 0,
            byte hitType = 0,
            byte range = 0,
            byte targetRange = 0,
            short upgradeSkill = 0,
            short upgradeType = 0,
            short castTime = 0,
            short cooldown = 0,
            short mpCost = 0,
            short itemVNum = 0,
            byte levelMinimum = 0,
            string fcombo = "0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0",
            (int dx, int dy)[]? cells = null,
            (int dx, int dy)[]? tailCells = null)
        {
            return $"\tVNUM\t{skillVNum}\r\n" +
                   $"\tNAME\t{name}\r\n" +
                   $"\tTYPE\t{skillType}\t{castId}\t{classType}\t{type}\t0\t{element}\t0\r\n" +
                   $"\tCOST\t{cpCost}\t{price}\t0{Triples(tailCells, 30)}\r\n" +
                   $"\tLEVEL\t{levelMinimum}\t-1\t-1\t-1\t-1\r\n" +
                   $"\tEFFECT\t0\t{castEffect}\t{castAnimation}\t{effect}\t{attackAnimation}\t0\t0\t0\t0\r\n" +
                   $"\tTARGET\t{targetType}\t{hitType}\t{range}\t{targetRange}\t0\r\n" +
                   $"\tDATA\t{upgradeSkill}\t{upgradeType}\t0\t0\t{castTime}\t{cooldown}\t0\t0\t{mpCost}\t0\t{itemVNum}\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t0\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t1\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t2\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t3\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t4\t0\t0\t0\t0\t0\r\n" +
                   $"\tFCOMBO\t{fcombo}\r\n" +
                   $"\tCELL\t8\t8{Triples(cells, 30)}\r\n" +
                   "\tZ_DESC\t0\r\n" +
                   "#=========================================================";
        }

        private static string Triples((int dx, int dy)[]? cells, int slots)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < slots; i++)
            {
                if ((cells != null) && (i < cells.Length))
                {
                    sb.Append($"\t{cells[i].dx}\t{cells[i].dy}\t1");
                }
                else
                {
                    sb.Append("\t0\t0\t0");
                }
            }

            return sb.ToString();
        }

        // A line of `length` cells straight in front of the caster: dy negative is forward.
        private static (int dx, int dy)[] Line(int length, int dx = 0)
        {
            var cells = new (int, int)[length];
            for (var i = 0; i < length; i++)
            {
                cells[i] = (dx, -length + i);
            }

            return cells;
        }

        private async Task<SkillDto> ParseOneAsync(string content)
        {
            CreateTestFile(content);
            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object,
                _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);
            Assert.AreEqual(1, _savedSkills.Count);
            return _savedSkills[0];
        }

        // An empty CELL is the overwhelming majority - 1890 of the 1958 skills - and has to come
        // out as nothing rather than as a pattern of zero-zero cells.
        [TestMethod]
        public async Task ASkillWithNoPatternHasNoCellPattern()
        {
            var skill = await ParseOneAsync(CreateSkillData(skillVNum: 1));
            Assert.IsNull(skill.CellPattern);
        }

        // Skill 244, the archer's piercing shot: the row of eight squares in front of the caster.
        [TestMethod]
        public async Task AStraightLineComesOutAsEightPairs()
        {
            var skill = await ParseOneAsync(CreateSkillData(skillVNum: 244, cells: Line(8)));
            Assert.AreEqual("0,-8,0,-7,0,-6,0,-5,0,-4,0,-3,0,-2,0,-1", skill.CellPattern);
        }

        // The list ends at the first triple whose third field is zero, not at the end of the row.
        [TestMethod]
        public async Task ThePatternStopsAtTheFirstZeroContinuesFlag()
        {
            var skill = await ParseOneAsync(CreateSkillData(skillVNum: 2, cells: Line(3)));
            Assert.AreEqual("0,-3,0,-2,0,-1", skill.CellPattern);
        }

        // CELL holds thirty cells and no more: 2 + 30*3 + 1 = 93 fields. Ten skills need more and
        // continue in the tail of COST, which is zeros for the other 1948.
        [TestMethod]
        public async Task APatternLongerThanThirtyContinuesInTheTailOfCost()
        {
            var skill = await ParseOneAsync(CreateSkillData(skillVNum: 1857,
                cells: Line(30), tailCells: Line(4, dx: 1)));

            Assert.AreEqual(34, skill.CellPattern!.Split(',').Length / 2);
            Assert.IsTrue(skill.CellPattern!.EndsWith("1,-4,1,-3,1,-2,1,-1"),
                "the tail is appended after the thirty CELL holds");
        }

        [TestMethod]
        public async Task AFullCellWithAnEmptyTailStopsAtThirty()
        {
            var skill = await ParseOneAsync(CreateSkillData(skillVNum: 1175, cells: Line(30)));
            Assert.AreEqual(30, skill.CellPattern!.Split(',').Length / 2);
        }

        [TestMethod]
        public async Task SkillParser_ReadsComboTripletsAfterTheSwitch()
        {
            // Skill 220's real row: the leading 1 is the switch, the triplets follow it.
            var content = CreateSkillData(skillVNum: 220, name: "Basic Slash",
                fcombo: "1\t3\t40\t513\t4\t25\t525\t5\t13\t524\t0\t0\t0\t0\t0\t0");
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object,
                _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            var combos = _savedCombos.Where(c => c.SkillVNum == 220).OrderBy(c => c.Hit).ToList();
            Assert.AreEqual(3, combos.Count);

            Assert.AreEqual(3, combos[0].Hit);
            Assert.AreEqual(40, combos[0].Animation);
            Assert.AreEqual(513, combos[0].Effect);

            Assert.AreEqual(4, combos[1].Hit);
            Assert.AreEqual(25, combos[1].Animation);
            Assert.AreEqual(525, combos[1].Effect);

            Assert.AreEqual(5, combos[2].Hit);
            Assert.AreEqual(13, combos[2].Animation);
            Assert.AreEqual(524, combos[2].Effect);

            Assert.IsTrue(combos.All(c => c.Hit < 10),
                "a hit number in the hundreds is a triplet read one field early");
        }

        [TestMethod]
        public async Task SkillParser_ASkillWithNoChainSavesNoSteps()
        {
            // The switch is 0 and every triplet is zero: nothing to save. Guards against the
            // switch itself being mistaken for a step.
            CreateTestFile(CreateSkillData(skillVNum: 1, name: "Fireball"));

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object,
                _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(0, _savedCombos.Count);
        }

        [TestMethod]
        public async Task SkillParser_ParsesSingleSkill()
        {
            var content = CreateSkillData(skillVNum: 1, name: "Fireball", mpCost: 50, cooldown: 10);
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(1, _savedSkills.Count);
            Assert.AreEqual(1, _savedSkills[0].SkillVNum);
            Assert.AreEqual("Fireball", _savedSkills[0].NameI18NKey);
            Assert.AreEqual(50, _savedSkills[0].MpCost);
            Assert.AreEqual(10, _savedSkills[0].Cooldown);
        }

        [TestMethod]
        public async Task SkillParser_ParsesMultipleSkills()
        {
            var content = CreateSkillData(skillVNum: 1, name: "Skill1") + "\n" +
                          CreateSkillData(skillVNum: 2, name: "Skill2") + "\n" +
                          CreateSkillData(skillVNum: 3, name: "Skill3");
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(3, _savedSkills.Count);
        }

        [TestMethod]
        public async Task SkillParser_ParsesTargetFields()
        {
            var content = CreateSkillData(skillVNum: 1, targetType: 1, hitType: 2, range: 5, targetRange: 3);
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(1, _savedSkills.Count);
            Assert.AreEqual(1, _savedSkills[0].TargetType);
            Assert.AreEqual(2, _savedSkills[0].HitType);
            Assert.AreEqual(5, _savedSkills[0].Range);
            Assert.AreEqual(3, _savedSkills[0].TargetRange);
        }

        [TestMethod]
        public async Task SkillParser_ParsesElementField()
        {
            var content = CreateSkillData(skillVNum: 1, element: 2);
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(1, _savedSkills.Count);
            Assert.AreEqual(2, _savedSkills[0].Element);
        }

        [TestMethod]
        public async Task SkillParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(0, _savedSkills.Count);
        }
    }
}
