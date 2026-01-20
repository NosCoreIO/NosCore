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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class PortalParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<PortalDto, int>> _portalDaoMock = null!;
        private Mock<IDao<MapDto, short>> _mapDaoMock = null!;
        private List<PortalDto> _savedPortals = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
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
                    new() { MapId = 189 }
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
