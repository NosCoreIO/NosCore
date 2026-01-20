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
    public class FluentParserBuilderTests
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
        public async Task FluentParser_WithExpressionExtractor_ParsesCorrectly()
        {
            var content = "VNUM\t1\t100\nNAME\tTestItem\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .Field(x => x.Id, chunk => Convert.ToInt32(chunk["VNUM"][0][1]))
                .Field(x => x.Price, chunk => Convert.ToInt64(chunk["VNUM"][0][2]))
                .Field(x => x.Name, chunk => chunk["NAME"][0][1])
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(100, results[0].Price);
            Assert.AreEqual("TestItem", results[0].Name);
        }

        [TestMethod]
        public async Task FluentParser_WithSimpleFieldMapping_ParsesCorrectly()
        {
            var content = "VNUM\t42\t999\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .Field(x => x.Id, "VNUM", 0, 1)
                .Field(x => x.Price, "VNUM", 0, 2)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(42, results[0].Id);
            Assert.AreEqual(999, results[0].Price);
        }

        [TestMethod]
        public async Task FluentParser_WithCustomConverter_ParsesCorrectly()
        {
            var content = "DATA\thello_world\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .Field(x => x.Name, "DATA", 0, 1, s => s.Replace("_", " "))
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("hello world", results[0].Name);
        }

        [TestMethod]
        public async Task FluentParser_WithCustomSplitter_ParsesCorrectly()
        {
            var content = "VNUM 1 100\nNAME Test Item\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .WithSplitter(" ")
                .Field(x => x.Id, "VNUM", 0, 1)
                .Field(x => x.Price, "VNUM", 0, 2)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].Id);
            Assert.AreEqual(100, results[0].Price);
        }

        [TestMethod]
        public async Task FluentParser_WithBooleanField_ParsesCorrectly()
        {
            var content = "DATA\t1\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDtoWithBool>.Create(filePath, "END", 0)
                .Field(x => x.IsActive, "DATA", 0, 1)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].IsActive);
        }

        [TestMethod]
        public async Task FluentParser_WithEnumField_ParsesCorrectly()
        {
            var content = "DATA\t2\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDtoWithEnum>.Create(filePath, "END", 0)
                .Field(x => x.Status, "DATA", 0, 1)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(TestStatus.Completed, results[0].Status);
        }

        [TestMethod]
        public async Task FluentParser_ParsesMultipleRecords()
        {
            var content = "VNUM\t1\nEND\nVNUM\t2\nEND\nVNUM\t3\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .Field(x => x.Id, "VNUM", 0, 1)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public async Task FluentParser_WithMethodReference_ParsesCorrectly()
        {
            var content = "DATA\t10\t20\t30\nEND";
            var filePath = CreateTestFile("test.dat", content);

            var parser = FluentParserBuilder<TestDto>.Create(filePath, "END", 0)
                .Field(x => x.Id, CalculateSum)
                .Build(_loggerMock.Object, _logLanguageMock.Object);

            var results = await parser.GetDtosAsync();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(60, results[0].Id);
        }

        private static object? CalculateSum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt32(chunk["DATA"][0][1]) +
                   Convert.ToInt32(chunk["DATA"][0][2]) +
                   Convert.ToInt32(chunk["DATA"][0][3]);
        }
    }

    public class TestDtoWithBool
    {
        public bool IsActive { get; set; }
    }

    public enum TestStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2
    }

    public class TestDtoWithEnum
    {
        public TestStatus Status { get; set; }
    }
}
