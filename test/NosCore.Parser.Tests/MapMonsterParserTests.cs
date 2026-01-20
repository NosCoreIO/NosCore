//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
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
    public class MapMonsterParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<MapMonsterDto, int>> _mapMonsterDaoMock = null!;
        private Mock<IDao<NpcMonsterDto, short>> _npcMonsterDaoMock = null!;
        private List<MapMonsterDto> _savedMonsters = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _mapMonsterDaoMock = new Mock<IDao<MapMonsterDto, int>>();
            _npcMonsterDaoMock = new Mock<IDao<NpcMonsterDto, short>>();
            _savedMonsters = [];

            _mapMonsterDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<MapMonsterDto>());

            _mapMonsterDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapMonsterDto>>()))
                .Callback<IEnumerable<MapMonsterDto>>(monsters => _savedMonsters.AddRange(monsters))
                .ReturnsAsync(true);

            _npcMonsterDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<NpcMonsterDto>
                {
                    new() { NpcMonsterVNum = 1 },
                    new() { NpcMonsterVNum = 2 },
                    new() { NpcMonsterVNum = 100 }
                });
        }

        [TestMethod]
        public async Task MapMonsterParser_ParsesSingleMonster()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "3", "1", "1001", "100", "100", "2", "100", "0" }
            };

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.AreEqual(1, _savedMonsters[0].MapId);
            Assert.AreEqual(1, _savedMonsters[0].VNum);
            Assert.AreEqual(1001, _savedMonsters[0].MapMonsterId);
            Assert.AreEqual(100, _savedMonsters[0].MapX);
            Assert.AreEqual(100, _savedMonsters[0].MapY);
        }

        [TestMethod]
        public async Task MapMonsterParser_ParsesMultipleMonsters()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "3", "1", "1001", "100", "100", "2", "100", "0" },
                new[] { "in", "3", "2", "1002", "110", "110", "3", "100", "0" }
            };

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(2, _savedMonsters.Count);
        }

        [TestMethod]
        public async Task MapMonsterParser_SkipsDuplicateMapMonsterId()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "3", "1", "1001", "100", "100", "2", "100", "0" },
                new[] { "in", "3", "1", "1001", "110", "110", "3", "100", "0" }
            };

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(1, _savedMonsters.Count);
        }

        [TestMethod]
        public async Task MapMonsterParser_SkipsUnknownMonsterVNum()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "3", "999", "1001", "100", "100", "2", "100", "0" }
            };

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(0, _savedMonsters.Count);
        }

        [TestMethod]
        public async Task MapMonsterParser_SetsIsMovingFromMvPackets()
        {
            var packets = new List<string[]>
            {
                new[] { "mv", "3", "1001", "100", "100", "5" },
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "3", "1", "1001", "100", "100", "2", "100", "0" }
            };

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(1, _savedMonsters.Count);
            Assert.IsTrue(_savedMonsters[0].IsMoving);
        }

        [TestMethod]
        public async Task MapMonsterParser_HandlesEmptyPacketList()
        {
            var packets = new List<string[]>();

            var parser = new MapMonsterParser(_mapMonsterDaoMock.Object, _npcMonsterDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapMonsterAsync(packets);

            Assert.AreEqual(0, _savedMonsters.Count);
        }
    }
}
