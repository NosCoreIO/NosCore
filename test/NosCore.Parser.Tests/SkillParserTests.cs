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
    public class SkillParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<SkillDto, short>> _skillDaoMock = null!;
        private Mock<IDao<BCardDto, short>> _bCardDaoMock = null!;
        private Mock<IDao<ComboDto, int>> _comboDaoMock = null!;
        private string _tempFolder = null!;
        private List<SkillDto> _savedSkills = null!;
        private List<BCardDto> _savedBCards = null!;
        private List<ComboDto> _savedCombos = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _skillDaoMock = new Mock<IDao<SkillDto, short>>();
            _bCardDaoMock = new Mock<IDao<BCardDto, short>>();
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
            byte levelMinimum = 0)
        {
            return $"\tVNUM\t{skillVNum}\r\n" +
                   $"\tNAME\t{name}\r\n" +
                   $"\tTYPE\t{skillType}\t{castId}\t{classType}\t{type}\t0\t{element}\t0\r\n" +
                   $"\tCOST\t{cpCost}\t{price}\t0\r\n" +
                   $"\tLEVEL\t{levelMinimum}\t-1\t-1\t-1\t-1\r\n" +
                   $"\tEFFECT\t0\t{castEffect}\t{castAnimation}\t{effect}\t{attackAnimation}\t0\t0\t0\t0\r\n" +
                   $"\tTARGET\t{targetType}\t{hitType}\t{range}\t{targetRange}\t0\r\n" +
                   $"\tDATA\t{upgradeSkill}\t{upgradeType}\t0\t0\t{castTime}\t{cooldown}\t0\t0\t{mpCost}\t0\t{itemVNum}\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t0\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t1\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t2\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t3\t0\t0\t0\t0\t0\r\n" +
                   "\tBASIC\t4\t0\t0\t0\t0\t0\r\n" +
                   "\tFCOMBO\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
                   "\tCELL\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
                   "\tZ_DESC\t0\r\n" +
                   "#=========================================================";
        }

        [TestMethod]
        public async Task SkillParser_ParsesSingleSkill()
        {
            var content = CreateSkillData(skillVNum: 1, name: "Fireball", mpCost: 50, cooldown: 10);
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
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

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(3, _savedSkills.Count);
        }

        [TestMethod]
        public async Task SkillParser_ParsesTargetFields()
        {
            var content = CreateSkillData(skillVNum: 1, targetType: 1, hitType: 2, range: 5, targetRange: 3);
            CreateTestFile(content);

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
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

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(1, _savedSkills.Count);
            Assert.AreEqual(2, _savedSkills[0].Element);
        }

        [TestMethod]
        public async Task SkillParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new SkillParser(_bCardDaoMock.Object, _comboDaoMock.Object, _skillDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertSkillsAsync(_tempFolder);

            Assert.AreEqual(0, _savedSkills.Count);
        }
    }
}
