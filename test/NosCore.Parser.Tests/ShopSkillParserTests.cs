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
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ShopSkillParserTests
    {
        private readonly List<ShopSkillDto> _saved = [];
        private ShopSkillParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var shopDao = new Mock<IDao<ShopDto, int>>();
            shopDao.Setup(x => x.LoadAll()).Returns(new List<ShopDto>
            {
                new() { ShopId = 12, MapNpcId = 1893 },
                new() { ShopId = 13, MapNpcId = 1894 }
            });
            var shopSkillDao = new Mock<IDao<ShopSkillDto, int>>();
            shopSkillDao.Setup(x => x.LoadAll()).Returns(new List<ShopSkillDto>());
            shopSkillDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShopSkillDto>>()))
                .Callback<IEnumerable<ShopSkillDto>>(_saved.AddRange)
                .ReturnsAsync(true);
            _parser = new ShopSkillParser(shopSkillDao.Object, shopDao.Object,
                new Mock<ILogger<ShopSkillParser>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task EachTabKeepsItsSkillsInOrder()
        {
            await _parser.InsertShopSkillsAsync(new List<string[]>
            {
                "n_inv 2 1893 0 0 20 24 28".Split(' '),
                "n_inv 2 1893 0 1 240 241".Split(' ')
            });

            CollectionAssert.AreEqual(new[] { "0:0:20", "0:1:24", "0:2:28", "1:0:240", "1:1:241" },
                _saved.Select(s => $"{s.Type}:{s.Slot}:{s.SkillVNum}").ToList());
            Assert.IsTrue(_saved.All(s => s.ShopId == 12));
        }

        // The class tab only lists the viewer's class, so each trace adds the skills it has not seen.
        [TestMethod]
        public async Task TracesOfTheSameTabAreMerged()
        {
            await _parser.InsertShopSkillsAsync(new List<string[]>
            {
                "n_inv 2 1893 0 1 220 221".Split(' '),
                "n_inv 2 1893 0 1 240 220".Split(' ')
            });

            CollectionAssert.AreEqual(new short[] { 220, 221, 240 }, _saved.Select(s => s.SkillVNum).ToList());
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, _saved.Select(s => s.Slot).ToList());
        }

        [TestMethod]
        public async Task ItemShopsAndUnknownNpcsAreSkipped()
        {
            await _parser.InsertShopSkillsAsync(new List<string[]>
            {
                "n_inv 2 1894 0 100 0.0.1012.-1.0.10".Split(' '),
                "n_inv 2 9999 0 0 20".Split(' ')
            });

            Assert.AreEqual(0, _saved.Count);
        }
    }
}
