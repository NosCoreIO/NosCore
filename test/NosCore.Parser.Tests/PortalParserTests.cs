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
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class PortalParserTests
    {
        private Mock<ILogger<PortalParser>> _loggerMock;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<PortalDto, int>> _portalDaoMock;
        private Mock<IDao<MapDto, short>> _mapDaoMock;
        private List<PortalDto> _savedPortals;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PortalParser>>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _portalDaoMock = new Mock<IDao<PortalDto, int>>();
            _mapDaoMock = new Mock<IDao<MapDto, short>>();
            _savedPortals = [];

            _portalDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<PortalDto>());

            _portalDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<PortalDto>()))
                .Callback<PortalDto>(portal => _savedPortals.Add(portal))
                .ReturnsAsync((PortalDto p) => p);

            _portalDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<PortalDto>>()))
                .Callback<IEnumerable<PortalDto>>(portals => _savedPortals.AddRange(portals))
                .ReturnsAsync(true);

            _mapDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<MapDto>
                {
                    new() { MapId = 1 },
                    new() { MapId = 2 },
                    new() { MapId = 3 },
                    new() { MapId = 150 },
                    new() { MapId = 98 },
                    new() { MapId = 20001 },
                    new() { MapId = 2586 },
                    new() { MapId = 145 },
                    new() { MapId = 2587 },
                    new() { MapId = 189 },
                    new() { MapId = 2006 },
                    new() { MapId = 2106 }
                });
        }

        [TestMethod]
        public async Task PortalParser_ParsesPortalsBetweenMaps()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "gp", "100", "100", "2", "0" },
                new[] { "at", "1", "2", "50", "50", "2", "0", "0", "0" },
                new[] { "gp", "100", "100", "1", "0" }
            };

            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(packets);

            var portalCount = _savedPortals.Count(p => p.SourceMapId == 1 || p.SourceMapId == 2);
            Assert.IsTrue(portalCount >= 2);
        }

        [TestMethod]
        public async Task PortalParser_CreatesHardcodedPortals()
        {
            var packets = new List<string[]>();

            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(packets);

            Assert.IsTrue(_savedPortals.Any(p => p.SourceMapId == 150));
            Assert.IsTrue(_savedPortals.Any(p => p.SourceMapId == 20001));
            Assert.IsTrue(_savedPortals.Any(p => p.SourceMapId == 2586));
            Assert.IsTrue(_savedPortals.Any(p => p.SourceMapId == 2587));
        }

        [TestMethod]
        public async Task PortalParser_CreatesTheMinilandEntrances()
        {
            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(new List<string[]>());

            var entrances = _savedPortals.Where(p => p.Type == PortalType.Miniland)
                .Select(p => (p.SourceMapId, p.SourceX, p.SourceY, p.DestinationMapId))
                .ToList();
            CollectionAssert.AreEquivalent(new List<(short, short, short, short)>
            {
                (1, 48, 132, 20001),
                (145, 9, 171, 20001),
            }, entrances);
        }

        [TestMethod]
        public async Task PortalParser_CreatesTheArenaExits()
        {
            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(new List<string[]>());

            var individual = _savedPortals.Single(p => p.SourceMapId == 2006);
            var glory = _savedPortals.Single(p => p.SourceMapId == 2106);
            Assert.AreEqual(((short)37, (short)15), (individual.SourceX, individual.SourceY));
            Assert.AreEqual(((short)38, (short)3), (glory.SourceX, glory.SourceY));
            Assert.IsTrue(new[] { individual, glory }.All(p =>
                p is { DestinationMapId: 1, DestinationX: 50, DestinationY: 66, Type: PortalType.MapPortal }));
        }

        [TestMethod]
        public async Task PortalParser_SkipsPortalsToUnknownMaps()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "gp", "100", "100", "999", "0" }
            };

            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(packets);

            Assert.IsFalse(_savedPortals.Any(p => p.DestinationMapId == 999));
        }

        [TestMethod]
        public async Task PortalParser_SkipsDuplicatePortals()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "gp", "100", "100", "2", "0" },
                new[] { "gp", "100", "100", "2", "0" }
            };

            var parser = new PortalParser(_loggerMock.Object, _mapDaoMock.Object, _portalDaoMock.Object, _logLanguageMock.Object);
            await parser.InsertPortalsAsync(packets);

            var duplicatePortals = _savedPortals.Count(p => p.SourceMapId == 1 && p.SourceX == 100 && p.SourceY == 100 && p.DestinationMapId == 2);
            Assert.IsTrue(duplicatePortals <= 1);
        }
    }
}
