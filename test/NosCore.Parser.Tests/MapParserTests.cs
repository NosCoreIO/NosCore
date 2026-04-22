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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MapParserTests
    {
        private Mock<IDao<MapDto, short>> _daoMock = null!;
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private string _tempFolder = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _daoMock = new Mock<IDao<MapDto, short>>();
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

        private void WriteMapIdDat(string content) =>
            File.WriteAllText(Path.Combine(_tempFolder, "MapIDData.dat"), content);

        [TestMethod]
        public async Task MapParser_ParsesSingleEntry()
        {
            WriteMapIdDat("1 1 0 0 nosvillage\r\nDATA 0\r\n");
            var parser = new MapParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].MapId);
            Assert.AreEqual("nosvillage", result[0].NameI18NKey);
        }

        [TestMethod]
        public async Task MapParser_ParsesMultipleEntries()
        {
            WriteMapIdDat(
                "1 1 0 0 nosvillage\r\nDATA 0\r\n" +
                "2 1 0 0 alveus\r\nDATA 0\r\n" +
                "145 1 0 0 oldnosville\r\nDATA 0\r\n");
            var parser = new MapParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(m => m.MapId == 1 && m.NameI18NKey == "nosvillage"));
            Assert.IsTrue(result.Any(m => m.MapId == 2 && m.NameI18NKey == "alveus"));
            Assert.IsTrue(result.Any(m => m.MapId == 145 && m.NameI18NKey == "oldnosville"));
        }

        [TestMethod]
        public async Task MapParser_EmptyFileReturnsEmptyList()
        {
            WriteMapIdDat("");
            var parser = new MapParser(_daoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(0, result.Count);
        }
    }
}
