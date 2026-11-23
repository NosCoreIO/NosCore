//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MapTypeMapParserTests
    {
        private List<MapTypeMapDto> _saved = [];

        private async Task ImportingMapTypesFor(params short[] mapIds)
        {
            var maps = new Mock<IDao<MapDto, short>>();
            maps.Setup(x => x.LoadAll()).Returns(mapIds.Select(id => new MapDto { MapId = id }).ToList());
            var mapTypeMaps = new Mock<IDao<MapTypeMapDto, short>>();
            mapTypeMaps.Setup(x => x.LoadAll()).Returns(new List<MapTypeMapDto>());
            mapTypeMaps.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapTypeMapDto>>()))
                .Callback<IEnumerable<MapTypeMapDto>>(m => _saved = m.ToList())
                .ReturnsAsync(true);
            await new MapTypeMapParser(mapTypeMaps.Object, maps.Object).InsertMapTypeMapsAsync();
        }

        private MapTypeType[] TypesOf(short mapId) =>
            _saved.Where(m => m.MapId == mapId).Select(m => m.MapTypeId).ToArray();

        [TestMethod]
        public async Task TheOasisTowerFloorsAboveMap300GetTheOasisType()
        {
            await ImportingMapTypesFor(1, 262, 2615, 2620);

            CollectionAssert.AreEqual(new[] { MapTypeType.Act1 }, TypesOf(1));
            CollectionAssert.AreEqual(new[] { MapTypeType.Oasis }, TypesOf(262));
            CollectionAssert.AreEqual(new[] { MapTypeType.Oasis }, TypesOf(2615));
            CollectionAssert.AreEqual(new[] { MapTypeType.Oasis }, TypesOf(2620));
        }

        [TestMethod]
        public async Task AMapNoRangeCoversGetsNoType()
        {
            await ImportingMapTypesFor(2614, 2621);

            Assert.AreEqual(0, _saved.Count);
        }
    }
}
