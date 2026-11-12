//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MapParserTests
    {
        private Mock<IDao<MapDto, short>> _daoMock;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private string _tempFolder;

        [TestInitialize]
        public void Setup()
        {
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
        public async Task MapParser_MarksTheArenas()
        {
            WriteMapIdDat("");
            var maps = Path.Combine(_tempFolder, "map");
            Directory.CreateDirectory(maps);
            foreach (var mapId in new[] { "1", "2006", "2106" })
            {
                File.WriteAllBytes(Path.Combine(maps, mapId), new byte[] { 1, 0, 1, 0, 0 });
            }

            List<MapDto> saved = [];
            _daoMock.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapDto>>()))
                .Callback<IEnumerable<MapDto>>(m => saved.AddRange(m))
                .ReturnsAsync(true);
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            await parser.InsertOrUpdateMapsAsync(_tempFolder, new List<string[]>());

            Assert.AreEqual(ArenaType.None, saved.Single(m => m.MapId == 1).Arena);
            Assert.AreEqual(ArenaType.Individual, saved.Single(m => m.MapId == 2006).Arena);
            Assert.AreEqual(ArenaType.Glory, saved.Single(m => m.MapId == 2106).Arena);
        }

        [TestMethod]
        public async Task MapParser_ParsesSingleEntry()
        {
            WriteMapIdDat("1 1 0 0 nosvillage\r\nDATA 0\r\n");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
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
                "2 2 0 0 alveus\r\nDATA 0\r\n" +
                "145 145 0 0 oldnosville\r\nDATA 0\r\n");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(m => m.MapId == 1 && m.NameI18NKey == "nosvillage"));
            Assert.IsTrue(result.Any(m => m.MapId == 2 && m.NameI18NKey == "alveus"));
            Assert.IsTrue(result.Any(m => m.MapId == 145 && m.NameI18NKey == "oldnosville"));
        }

        [TestMethod]
        public async Task MapParser_KeepsAnEntryFollowedByANonZeroRegion()
        {
            WriteMapIdDat(
                "170 170 2 3 desert\rDATA 1\r\r" +
                "171 171 2 4 portsurroundings\rDATA 1\r\r" +
                "172 172 2 5 neilscamp\rDATA 0\r");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(m => m.MapId == 171 && m.NameI18NKey == "portsurroundings"));
        }

        [TestMethod]
        public async Task MapParser_NamesEveryMapOfARange()
        {
            WriteMapIdDat("# startMapId endMapId\r4000 4002 1 -1 arena\rDATA 0\r");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            CollectionAssert.AreEquivalent(new short[] { 4000, 4001, 4002 }, result.Select(m => m.MapId).ToArray());
            Assert.IsTrue(result.All(m => m.NameI18NKey == "arena"));
        }

        [TestMethod]
        public async Task MapParser_KeepsTheFirstLineForARepeatedMap()
        {
            WriteMapIdDat("2592 2592 1 -1 first\rDATA 0\r2592 2592 1 -1 second\rDATA 0\r");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("first", result[0].NameI18NKey);
        }

        [TestMethod]
        public async Task MapParser_EmptyFileReturnsEmptyList()
        {
            WriteMapIdDat("");
            var parser = new MapParser(_daoMock.Object, NullLoggerFactory.Instance, _logLanguageMock.Object);
            var result = await parser.ParseDatAsync(_tempFolder);

            Assert.AreEqual(0, result.Count);
        }
    }
}
