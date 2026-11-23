//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MapTypeParserTests
    {
        private readonly List<MapTypeDto> _saved = new();
        private readonly List<RespawnMapTypeDto> _savedRespawns = new();

        [TestInitialize]
        public async Task Setup()
        {
            var mapTypes = new Mock<IDao<MapTypeDto, MapTypeType>>();
            mapTypes.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MapTypeDto>>()))
                .Callback<IEnumerable<MapTypeDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);
            var respawns = new Mock<IDao<RespawnMapTypeDto, RespawnType>>();
            respawns.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<RespawnMapTypeDto>>()))
                .Callback<IEnumerable<RespawnMapTypeDto>>(rows => _savedRespawns.AddRange(rows))
                .ReturnsAsync(true);
            var logLanguage = new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object;
            await new MapTypeParser(mapTypes.Object, NullLogger<MapTypeParser>.Instance, logLanguage).InsertMapTypesAsync();
            await new RespawnMapTypeParser(respawns.Object, NullLogger<RespawnMapTypeParser>.Instance, logLanguage).InsertRespawnMapTypeAsync();
        }

        [TestMethod]
        public void EachMapTypeIsStoredOnce() =>
            Assert.AreEqual(_saved.Count, _saved.Select(m => m.MapTypeId).Distinct().Count());

        [TestMethod]
        public void EachRespawnTypeIsStoredOnce() =>
            Assert.AreEqual(_savedRespawns.Count, _savedRespawns.Select(r => r.RespawnMapTypeId).Distinct().Count());

        [TestMethod]
        public void EveryRespawnAMapTypePointsToIsStored()
        {
            var stored = _savedRespawns.Select(r => r.RespawnMapTypeId).ToHashSet();
            foreach (var mapType in _saved)
            {
                Assert.IsTrue(mapType.RespawnMapTypeId is null || stored.Contains(mapType.RespawnMapTypeId.Value), mapType.MapTypeName);
                Assert.IsTrue(mapType.ReturnMapTypeId is null || stored.Contains(mapType.ReturnMapTypeId.Value), mapType.MapTypeName);
            }
        }

        [TestMethod]
        public void Act4MapsHaveALongPotionDelayAndNoRespawn()
        {
            foreach (var act4 in _saved.Where(m => m.MapTypeId is MapTypeType.Act4 or MapTypeType.Act42))
            {
                Assert.AreEqual((short)5000, act4.PotionDelay);
                Assert.IsNull(act4.RespawnMapTypeId);
            }
        }

        [TestMethod]
        public void Act1RespawnsInNosville()
        {
            var act1 = _saved.Single(m => m.MapTypeId == MapTypeType.Act1);
            Assert.AreEqual(RespawnType.DefaultAct1, act1.RespawnMapTypeId);
            var respawn = _savedRespawns.Single(r => r.RespawnMapTypeId == RespawnType.DefaultAct1);
            Assert.AreEqual(((short)1, (short)80, (short)116), (respawn.MapId, respawn.DefaultX, respawn.DefaultY));
        }
    }
}
