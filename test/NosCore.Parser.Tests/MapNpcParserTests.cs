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
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MapNpcParserTests
    {
        private Mock<ILogger<MapNpcParser>> _loggerMock;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<MapNpcDto, int>> _mapNpcDaoMock;
        private Mock<IDao<NpcMonsterDto, short>> _npcMonsterDaoMock;
        private Mock<IDao<NpcTalkDto, short>> _npcTalkDaoMock;
        private List<MapNpcDto> _savedNpcs;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MapNpcParser>>();
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
                    new() { NpcMonsterVNum = 1, CanWalk = true },
                    new() { NpcMonsterVNum = 2, CanWalk = true },
                    new() { NpcMonsterVNum = 100, CanWalk = true }
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
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, 2, dialog: 1)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
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
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, 2, dialog: 1),
                Captured.Npc(2, 101, 70, 80, 3, dialog: 2)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(2, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SkipsDuplicateMapNpcId()
        {
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, 2, dialog: 1),
                Captured.Npc(1, 100, 70, 80, 3, dialog: 2)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SkipsUnknownNpcVNum()
        {
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(999, 100, 50, 60, 2, dialog: 1)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(0, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_SetsDialogFromNpcTalk()
        {
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, 2, dialog: 1)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
            Assert.AreEqual((short)1, _savedNpcs[0].Dialog);
        }

        [TestMethod]
        public async Task MapNpcParser_KeepsAnNpcSitting()
        {
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, sitting: true),
                Captured.Npc(2, 101, 70, 80)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            CollectionAssert.AreEqual(new[] { true, false }, _savedNpcs.Select(s => s.IsSitting).ToList());
        }

        [TestMethod]
        public async Task MapNpcParser_TakesTheAuraFromTheMonsterData()
        {
            _npcMonsterDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<NpcMonsterDto> { new() { NpcMonsterVNum = 955, EffectIdConstantly = 857 } });
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(955, 100, 50, 60)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual((short)857, _savedNpcs.Single().Effect);
        }

        [TestMethod]
        public async Task MapNpcParser_UpdatesAnNpcAlreadyImported()
        {
            _mapNpcDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<MapNpcDto> { new() { MapNpcId = 100, MapId = 1, VNum = 1 } });
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Npc(1, 100, 50, 60, dialog: 1)
            };

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(1, _savedNpcs.Count);
        }

        [TestMethod]
        public async Task MapNpcParser_HandlesEmptyPacketList()
        {
            var packets = new List<IPacket>();

            var parser = new MapNpcParser(_mapNpcDaoMock.Object, _npcMonsterDaoMock.Object, _npcTalkDaoMock.Object, new Mock<IDao<ShopDto, int>>().Object,
                new Mock<IDao<ShopItemDto, int>>().Object, new Mock<IDao<ShopSkillDto, int>>().Object, new Mock<IDao<TeleporterDto, short>>().Object,
                new Mock<IDao<RecipeDto, short>>().Object, new Mock<IDao<RecipeItemDto, short>>().Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertMapNpcsAsync(packets);

            Assert.AreEqual(0, _savedNpcs.Count);
        }
    }
}
