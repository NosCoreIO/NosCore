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
    public class MapNpcParserTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock = null!;
        private Mock<IDao<MapNpcDto, int>> _mapNpcDaoMock = null!;
        private Mock<IDao<NpcMonsterDto, short>> _npcMonsterDaoMock = null!;
        private Mock<IDao<NpcTalkDto, short>> _npcTalkDaoMock = null!;
        private List<MapNpcDto> _savedNpcs = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _mapNpcDaoMock = new Mock<IDao<MapNpcDto, int>>();
            _npcMonsterDaoMock = new Mock<IDao<NpcMonsterDto, short>>();
            _npcTalkDaoMock = new Mock<IDao<NpcTalkDto, short>>();
            _savedNpcs = [];

            _mapNpcDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<MapNpcDto>());

            _mapNpcDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapNpcDto>>()))
                .Callback<IEnumerable<MapNpcDto>>(npcs => _savedNpcs.AddRange(npcs))
                .ReturnsAsync(true);

            _npcMonsterDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<NpcMonsterDto>
                {
                    new() { NpcMonsterVNum = 1 },
                    new() { NpcMonsterVNum = 2 },
                    new() { NpcMonsterVNum = 100 }
                });

            _npcTalkDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<NpcTalkDto>
                {
                    new() { DialogId = 1 },
                    new() { DialogId = 2 }
                });
        }

        [TestMethod]
        public async Task MapNpcParser_ParsesSingleNpc()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "2", "1", "100", "50", "60", "2", "100", "0", "1", "0", "0", "0", "1", "0" }
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
            Assert.AreEqual(1, _savedNpcs[0].MapId);
            Assert.AreEqual(1, _savedNpcs[0].VNum);
            Assert.AreEqual(100, _savedNpcs[0].MapNpcId);
            Assert.AreEqual(50, _savedNpcs[0].MapX);
            Assert.AreEqual(60, _savedNpcs[0].MapY);
        }

        [TestMethod]
        public async Task MapNpcParser_ParsesMultipleNpcs()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "2", "1", "100", "50", "60", "2", "100", "0", "1", "0", "0", "0", "1", "0" },
                new[] { "in", "2", "2", "101", "70", "80", "3", "100", "0", "2", "0", "0", "0", "1", "0" }
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(2, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SkipsDuplicateMapNpcId()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "2", "1", "100", "50", "60", "2", "100", "0", "1", "0", "0", "0", "1", "0" },
                new[] { "in", "2", "1", "100", "70", "80", "3", "100", "0", "2", "0", "0", "0", "1", "0" }
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SkipsUnknownNpcVNum()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "2", "999", "100", "50", "60", "2", "100", "0", "1", "0", "0", "0", "1", "0" }
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(0, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SetsDialogFromNpcTalk()
        {
            var packets = new List<string[]>
            {
                new[] { "at", "1", "1", "50", "50", "2", "0", "0", "0" },
                new[] { "in", "2", "1", "100", "50", "60", "2", "100", "0", "1", "0", "0", "0", "1", "0" }
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
            Assert.AreEqual((short)1, _savedNpcs[0].Dialog);
        }

        [TestMethod]
        public async Task MapNpcParser_HandlesEmptyPacketList()
        {
            var packets = new List<string[]>();

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(0, _savedNpcs.Count);
        }
    }
}
