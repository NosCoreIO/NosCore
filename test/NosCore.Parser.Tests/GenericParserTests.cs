//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class GenericParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
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

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_tempFolder, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        [TestMethod]
        public async Task GenericParser_ParsesSingleRecord()
        {
            var content = "VNUM\t1\t100\nNAME\tTestItem\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                { nameof(TestDto.Id), chunk => Convert.ToInt32(chunk["VNUM"][0][1]) },
                { nameof(TestDto.Price), chunk => Convert.ToInt64(chunk["VNUM"][0][2]) },
                { nameof(TestDto.Name), chunk => chunk["NAME"][0][1] }
            };

            var parser = new GenericParser<TestDto>(filePath, "END", 0, actionList, _loggerMock.Object, _logLanguageMock.Object);
            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(100, results[0].Price);
            Assert.AreEqual("TestItem", results[0].Name);
        }

        [TestMethod]
        public async Task GenericParser_ParsesMultipleRecords()
        {
            var content = "VNUM\t1\t100\nNAME\tItem1\nEND\nVNUM\t2\t200\nNAME\tItem2\nEND\nVNUM\t3\t300\nNAME\tItem3\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                { nameof(TestDto.Id), chunk => Convert.ToInt32(chunk["VNUM"][0][1]) },
                { nameof(TestDto.Price), chunk => Convert.ToInt64(chunk["VNUM"][0][2]) },
                { nameof(TestDto.Name), chunk => chunk["NAME"][0][1] }
            };

            var parser = new GenericParser<TestDto>(filePath, "END", 0, actionList, _loggerMock.Object, _logLanguageMock.Object);
            var results = await parser.GetDtosAsync();

            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public async Task GenericParser_HandlesEmptyFile()
        {
            var content = "";
            var filePath = CreateTestFile("test.dat", content);

            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                { nameof(TestDto.Id), chunk => Convert.ToInt32(chunk["VNUM"][0][1]) }
            };

            var parser = new GenericParser<TestDto>(filePath, "END", 0, actionList, _loggerMock.Object, _logLanguageMock.Object);
            var results = await parser.GetDtosAsync();

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task GenericParser_HandlesMultipleLinesWithSameKey()
        {
            var content = "DATA\t1\t100\nDATA\t2\t200\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                { nameof(TestDto.Id), chunk => Convert.ToInt32(chunk["DATA"][0][1]) },
                { nameof(TestDto.Price), chunk => Convert.ToInt64(chunk["DATA"][1][2]) }
            };

            var parser = new GenericParser<TestDto>(filePath, "END", 0, actionList, _loggerMock.Object, _logLanguageMock.Object);
            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(200, results[0].Price);
        }

        [TestMethod]
        public async Task GenericParser_UsesCustomSplitter()
        {
            var content = "VNUM 1 100\nNAME TestItem\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                { nameof(TestDto.Id), chunk => Convert.ToInt32(chunk["VNUM"][0][1]) },
                { nameof(TestDto.Price), chunk => Convert.ToInt64(chunk["VNUM"][0][2]) },
                { nameof(TestDto.Name), chunk => chunk["NAME"][0][1] }
            };

            var parser = new GenericParser<TestDto>(filePath, "END", 0, actionList, _loggerMock.Object, _logLanguageMock.Object);
            var results = await parser.GetDtosAsync(" ");

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(100, results[0].Price);
            Assert.AreEqual("TestItem", results[0].Name);
        }
    }

    public class TestDto
    {
        public int Id { get; set; }
        public long Price { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
