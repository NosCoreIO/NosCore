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
    public class DropParserTests
    {
        private Mock<IDao<DropDto, short>> _dropDaoMock = null!;
        private List<DropDto> _savedDrops = null!;

        [TestInitialize]
        public void Setup()
        {
            _dropDaoMock = new Mock<IDao<DropDto, short>>();
            _savedDrops = [];
            _dropDaoMock
                .Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<DropDto>>()))
                .Callback<IEnumerable<DropDto>>(d => _savedDrops.AddRange(d))
                .ReturnsAsync(true);
        }

        [TestMethod]
        public void GenerateDropDtoPropagatesAllFields()
        {
            var parser = new DropParser(_dropDaoMock.Object);

            var dto = parser.GenerateDropDto(
                vnum: 1012,
                amount: 3,
                monsterVNum: 42,
                dropChance: 1500,
                mapTypeId: (short)MapTypeType.Act1);

            Assert.AreEqual<short>(1012, dto.VNum);
            Assert.AreEqual(3, dto.Amount);
            Assert.AreEqual((short?)42, dto.MonsterVNum);
            Assert.AreEqual(1500, dto.DropChance);
            Assert.AreEqual((short?)MapTypeType.Act1, dto.MapTypeId);
        }

        [TestMethod]
        public void GenerateDropDtoAcceptsNullMonsterVNumForMapWideDrops()
        {
            var parser = new DropParser(_dropDaoMock.Object);

            var dto = parser.GenerateDropDto(
                vnum: 2282,
                amount: 1,
                monsterVNum: null,
                dropChance: 2500,
                mapTypeId: (short)MapTypeType.Act2);

            Assert.IsNull(dto.MonsterVNum);
            Assert.AreEqual<short>(2282, dto.VNum);
        }

        [TestMethod]
        public async Task InsertDropAsyncEmitsDropsForEveryMajorActMapType()
        {
            var parser = new DropParser(_dropDaoMock.Object);
            await parser.InsertDropAsync();

            foreach (var mapType in new[]
            {
                MapTypeType.Act1, MapTypeType.Act2, MapTypeType.Act3, MapTypeType.Act32,
                MapTypeType.Act4, MapTypeType.Act42, MapTypeType.Act51, MapTypeType.Act52,
                MapTypeType.Act61A, MapTypeType.Act61D, MapTypeType.Act62, MapTypeType.Oasis,
                MapTypeType.Mine1, MapTypeType.Mine2, MapTypeType.MeadowOfMine,
                MapTypeType.SunnyPlain, MapTypeType.Fernon, MapTypeType.FernonF,
                MapTypeType.Cliff, MapTypeType.CometPlain, MapTypeType.LandOfTheDead,
            })
            {
                Assert.IsTrue(_savedDrops.Any(d => d.MapTypeId == (short)mapType),
                    $"No drops emitted for MapType {mapType}");
            }
        }

        [TestMethod]
        public async Task InsertDropAsyncEmitsGoldDropOnEveryMajorAct()
        {
            var parser = new DropParser(_dropDaoMock.Object);
            await parser.InsertDropAsync();

            const short goldVNum = 1012;
            foreach (var mapType in new[]
            {
                MapTypeType.Act1, MapTypeType.Act2, MapTypeType.Act3, MapTypeType.Act4,
                MapTypeType.Act51, MapTypeType.Act52, MapTypeType.Act61A, MapTypeType.Act61D,
                MapTypeType.Act62,
            })
            {
                Assert.IsTrue(_savedDrops.Any(d => d.VNum == goldVNum && d.MapTypeId == (short)mapType),
                    $"Gold (1012) should drop on {mapType}");
            }
        }
    }
}
