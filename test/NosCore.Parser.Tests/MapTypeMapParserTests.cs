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
        private readonly List<MapDto> _maps = new();
        private readonly List<MapTypeMapDto> _existing = new();
        private readonly List<MapTypeMapDto> _saved = new();
        private MapTypeMapParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var mapTypeMaps = new Mock<IDao<MapTypeMapDto, short>>();
            mapTypeMaps.Setup(s => s.LoadAll()).Returns(_existing);
            mapTypeMaps.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapTypeMapDto>>()))
                .Callback<IEnumerable<MapTypeMapDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);
            var maps = new Mock<IDao<MapDto, short>>();
            maps.Setup(s => s.LoadAll()).Returns(_maps);
            _parser = new MapTypeMapParser(mapTypeMaps.Object, maps.Object);
        }

        private void GivenMaps(params short[] ids) => _maps.AddRange(ids.Select(id => new MapDto { MapId = id }));

        private List<MapTypeType> TypesOf(short mapId) =>
            _saved.Where(s => s.MapId == mapId).Select(s => s.MapTypeId).ToList();

        [TestMethod]
        [DataRow((short)1, MapTypeType.Act1)]
        [DataRow((short)3, MapTypeType.MeadowOfMine)]
        [DataRow((short)20, MapTypeType.Act2)]
        [DataRow((short)41, MapTypeType.Act3)]
        [DataRow((short)103, MapTypeType.CometPlain)]
        [DataRow((short)138, MapTypeType.CleftOfDarkness)]
        [DataRow((short)150, MapTypeType.LandOfTheDead)]
        [DataRow((short)153, MapTypeType.Act42)]
        [DataRow((short)170, MapTypeType.Act51)]
        [DataRow((short)205, MapTypeType.Act52)]
        [DataRow((short)229, MapTypeType.Act61)]
        [DataRow((short)240, MapTypeType.Act62)]
        [DataRow((short)260, MapTypeType.Act32)]
        [DataRow((short)261, MapTypeType.Oasis)]
        public async Task AMapGetsTheTypeOfItsRange(short mapId, MapTypeType type)
        {
            GivenMaps(mapId);

            await _parser.InsertMapTypeMapsAsync();

            CollectionAssert.AreEqual(new[] { type }, TypesOf(mapId));
        }

        [TestMethod]
        public async Task TheCitadelsAreAct4MapsOfTheirFaction()
        {
            GivenMaps(130, 131);

            await _parser.InsertMapTypeMapsAsync();

            CollectionAssert.AreEquivalent(new[] { MapTypeType.Act4, MapTypeType.CitadelAngel }, TypesOf(130));
            CollectionAssert.AreEquivalent(new[] { MapTypeType.Act4, MapTypeType.CitadelDemon }, TypesOf(131));
        }

        [TestMethod]
        public async Task MapsMissingFromTheDatabaseAreSkipped()
        {
            GivenMaps(1);

            await _parser.InsertMapTypeMapsAsync();

            CollectionAssert.AreEqual(new short[] { 1 }, _saved.Select(s => s.MapId).ToList());
        }

        [TestMethod]
        public async Task MapsWithoutAKnownTypeAreSkipped()
        {
            GivenMaps(19, 298);

            await _parser.InsertMapTypeMapsAsync();

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ARerunAddsOnlyMissingPairs()
        {
            GivenMaps(1, 2);
            _existing.Add(new MapTypeMapDto { MapId = 1, MapTypeId = MapTypeType.Act1 });

            await _parser.InsertMapTypeMapsAsync();

            CollectionAssert.AreEqual(new short[] { 2 }, _saved.Select(s => s.MapId).ToList());
        }
    }
}
