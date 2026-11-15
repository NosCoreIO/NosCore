//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ShopItemParserTests
    {
        private readonly List<ShopItemDto> _saved = [];
        private ShopItemParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var shopDao = new Mock<IDao<ShopDto, int>>();
            shopDao.Setup(x => x.LoadAll()).Returns(new List<ShopDto> { new() { ShopId = 12, MapNpcId = 1893 } });
            var shopItemDao = new Mock<IDao<ShopItemDto, int>>();
            shopItemDao.Setup(x => x.LoadAll()).Returns(new List<ShopItemDto>());
            shopItemDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShopItemDto>>()))
                .Callback<IEnumerable<ShopItemDto>>(_saved.AddRange)
                .ReturnsAsync(true);
            _parser = new ShopItemParser(shopItemDao.Object, shopDao.Object,
                new Mock<ILogger<ShopItemParser>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task ItemsAreStoredUnderTheTabOpenedBeforeThem()
        {
            await _parser.InsertShopItemsAsync([
                Captured.Shopping(1, 1893),
                Captured.ItemShop(1893,
                    new NInvItemSubPacket { Type = PocketType.Equipment, Slot = 3, VNum = 1012, RareAmount = 7, UpgradeDesign = 5, Price = 10 },
                    new NInvItemSubPacket { Type = PocketType.Main, Slot = 5, VNum = 2024, RareAmount = 99, Price = 30 })
            ]);

            CollectionAssert.AreEqual(new[] { (1, 0, 1012, 7, 5), (1, 1, 2024, 0, 0) },
                _saved.Select(s => ((int)s.Type, (int)s.Slot, (int)s.ItemVNum, (int)s.Rare, (int)s.Upgrade)).ToList());
        }

        [TestMethod]
        public async Task AnEmptySlotIsSkipped()
        {
            await _parser.InsertShopItemsAsync([
                new NInvPacket
                {
                    VisualType = NosCore.Shared.Enumerations.VisualType.Npc,
                    VisualId = 1893,
                    ShopKind = 100,
                    Items = [null, new NInvItemSubPacket { Type = PocketType.Main, Slot = 1, VNum = 2024, RareAmount = 1, Price = 30 }]
                }
            ]);

            Assert.AreEqual(2024, _saved.Single().ItemVNum);
        }
    }
}
