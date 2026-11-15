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
    public class ShopParserTests
    {
        private Mock<ILogger<ShopParser>> _loggerMock;
        private Mock<ILogLanguageLocalizer<LogLanguageKey>> _logLanguageMock;
        private Mock<IDao<ShopDto, int>> _shopDaoMock;
        private Mock<IDao<MapNpcDto, int>> _mapNpcDaoMock;
        private List<ShopDto> _savedShops;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<ShopParser>>();
            _logLanguageMock = new Mock<ILogLanguageLocalizer<LogLanguageKey>>();
            _shopDaoMock = new Mock<IDao<ShopDto, int>>();
            _mapNpcDaoMock = new Mock<IDao<MapNpcDto, int>>();
            _savedShops = [];

            _shopDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<ShopDto>());

            _shopDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShopDto>>()))
                .Callback<IEnumerable<ShopDto>>(shops => _savedShops.AddRange(shops))
                .ReturnsAsync(true);

            _mapNpcDaoMock
                .Setup(x => x.LoadAll())
                .Returns(new List<MapNpcDto>
                {
                    new() { MapNpcId = 100, VNum = 1 },
                    new() { MapNpcId = 101, VNum = 2 },
                    new() { MapNpcId = 102, VNum = 3 }
                });
        }

        [TestMethod]
        public async Task ShopParser_ParsesSingleShop()
        {
            var packets = new List<IPacket>
            {
                Captured.Shop(100, 1, 0, "Shop Name")
            };

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(1, _savedShops.Count);
            Assert.AreEqual(100, _savedShops[0].MapNpcId);
            Assert.AreEqual(1, _savedShops[0].MenuType);
            Assert.AreEqual(0, _savedShops[0].ShopType);
        }

        [TestMethod]
        public async Task ShopParser_ParsesMultipleShops()
        {
            var packets = new List<IPacket>
            {
                Captured.Shop(100, 1, 0, "Shop1"),
                Captured.Shop(101, 2, 1, "Shop2")
            };

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(2, _savedShops.Count);
        }

        [TestMethod]
        public async Task ShopParser_SkipsDuplicateMapNpcId()
        {
            var packets = new List<IPacket>
            {
                Captured.Shop(100, 1, 0, "Shop1"),
                Captured.Shop(100, 2, 1, "Shop1Duplicate")
            };

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(1, _savedShops.Count);
        }

        [TestMethod]
        public async Task ShopParser_SkipsUnknownNpcId()
        {
            var packets = new List<IPacket>
            {
                Captured.Shop(999, 1, 0, "UnknownShop")
            };

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(0, _savedShops.Count);
        }

        [TestMethod]
        public async Task ShopParser_HandlesEmptyPacketList()
        {
            var packets = new List<IPacket>();

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(0, _savedShops.Count);
        }

        [TestMethod]
        public async Task ShopParser_IgnoresNonShopPackets()
        {
            var packets = new List<IPacket>
            {
                Captured.Arrival(1),
                Captured.Shop(100, 1, 0, "Shop1")
            };

            var parser = new ShopParser(_shopDaoMock.Object, _mapNpcDaoMock.Object, _loggerMock.Object, _logLanguageMock.Object);
            await parser.InsertShopsAsync(packets);

            Assert.AreEqual(1, _savedShops.Count);
        }
    }
}
