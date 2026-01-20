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
    public class ActParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<ActDto, byte>> _actDaoMock = null!;
        private Mock<IDao<ActPartDto, byte>> _actPartDaoMock = null!;
        private string _tempFolder = null!;
        private List<ActDto> _savedActs = null!;
        private List<ActPartDto> _savedActParts = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _actDaoMock = new Mock<IDao<ActDto, byte>>();
            _actPartDaoMock = new Mock<IDao<ActPartDto, byte>>();
            _savedActs = [];
            _savedActParts = [];

            _actDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ActDto>>()))
                .Callback<IEnumerable<ActDto>>(acts => _savedActs.AddRange(acts))
                .ReturnsAsync(true);

            _actPartDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ActPartDto>>()))
                .Callback<IEnumerable<ActPartDto>>(parts => _savedActParts.AddRange(parts))
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
            File.WriteAllText(Path.Combine(_tempFolder, "act_desc.dat"), content);
        }

        [TestMethod]
        public async Task ActParser_ParsesActLines()
        {
            var content = "A\t1\tzts1e\nA\t2\tzts2e\n~";
            CreateTestFile(content);

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(2, _savedActs.Count);
            Assert.AreEqual(1, _savedActs[0].ActId);
            Assert.AreEqual("zts1e", _savedActs[0].TitleI18NKey);
            Assert.AreEqual(40, _savedActs[0].Scene);
            Assert.AreEqual(2, _savedActs[1].ActId);
            Assert.AreEqual("zts2e", _savedActs[1].TitleI18NKey);
            Assert.AreEqual(41, _savedActs[1].Scene);
        }

        [TestMethod]
        public async Task ActParser_ParsesDataLines()
        {
            var content = "Data 1 1 1 10\nData 2 1 2 6\nend\n~";
            CreateTestFile(content);

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(2, _savedActParts.Count);
            Assert.AreEqual(1, _savedActParts[0].ActPartId);
            Assert.AreEqual(1, _savedActParts[0].ActId);
            Assert.AreEqual(1, _savedActParts[0].ActPartNumber);
            Assert.AreEqual(10, _savedActParts[0].MaxTs);
            Assert.AreEqual(2, _savedActParts[1].ActPartId);
            Assert.AreEqual(1, _savedActParts[1].ActId);
            Assert.AreEqual(2, _savedActParts[1].ActPartNumber);
            Assert.AreEqual(6, _savedActParts[1].MaxTs);
        }

        [TestMethod]
        public async Task ActParser_ParsesMixedContent()
        {
            var content = @"# Act Data
#===================================
Data 1 1 1 10
Data 2 1 2 6
Data 7 2 1 3
end
#==================================#
# Title
A	1	zts1e
A	2	zts2e
~";
            CreateTestFile(content);

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(2, _savedActs.Count);
            Assert.AreEqual(3, _savedActParts.Count);
        }

        [TestMethod]
        public async Task ActParser_IgnoresCommentLines()
        {
            var content = "# This is a comment\nA\t1\tzts1e\n# Another comment\n~";
            CreateTestFile(content);

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(1, _savedActs.Count);
            Assert.AreEqual(0, _savedActParts.Count);
        }

        [TestMethod]
        public async Task ActParser_HandlesEmptyFile()
        {
            CreateTestFile("");

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(0, _savedActs.Count);
            Assert.AreEqual(0, _savedActParts.Count);
        }

        [TestMethod]
        public async Task ActParser_SceneIsCalculatedFromActId()
        {
            var content = "A\t5\tzts5e\n~";
            CreateTestFile(content);

            var parser = new ActParser(_actDaoMock.Object, _actPartDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.ImportActAsync(_tempFolder);

            Assert.AreEqual(1, _savedActs.Count);
            Assert.AreEqual(5, _savedActs[0].ActId);
            Assert.AreEqual(44, _savedActs[0].Scene); // 39 + 5
        }
    }
}
