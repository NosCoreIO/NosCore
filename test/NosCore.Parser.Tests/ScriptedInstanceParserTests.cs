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
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class ScriptedInstanceParserTests
    {
        private Mock<IDao<MapDto, short>> _mapDao;
        private Mock<IDao<ScriptedInstanceDto, short>> _instanceDao;
        private List<ScriptedInstanceDto> _saved;
        private List<ScriptedInstanceDto> _existing;
        private ScriptedInstanceParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _saved = [];
            _existing = [];

            _mapDao = new Mock<IDao<MapDto, short>>();
            _mapDao.Setup(s => s.LoadAll()).Returns(new List<MapDto>
            {
                new() { MapId = 1 }, new() { MapId = 2 }, new() { MapId = 132 },
                new() { MapId = 133 }, new() { MapId = 2500 }, new() { MapId = 2520 }
            });

            _instanceDao = new Mock<IDao<ScriptedInstanceDto, short>>();
            _instanceDao.Setup(s => s.LoadAll()).Returns(() => _existing);
            _instanceDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ScriptedInstanceDto>>()))
                .Callback<IEnumerable<ScriptedInstanceDto>>(rows => _saved.AddRange(rows))
                .ReturnsAsync(true);

            _parser = new ScriptedInstanceParser(new Mock<ILogger<ScriptedInstanceParser>>().Object,
                _mapDao.Object, _instanceDao.Object,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        private static List<IPacket> Capture(params IPacket[] packets) => [.. packets];

        [TestMethod]
        public async Task AWaypointBecomesATimeSpaceEntranceOnTheMapItFollowsAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99)));

            Assert.AreEqual(1, _saved.Count);
            var entrance = _saved[0];
            Assert.AreEqual(1, entrance.MapId);
            Assert.AreEqual(134, entrance.PositionX);
            Assert.AreEqual(36, entrance.PositionY);
            Assert.AreEqual(ScriptedInstanceType.TimeSpace, entrance.Type);
            Assert.AreEqual(1, entrance.LevelMinimum);
            Assert.AreEqual(99, entrance.LevelMaximum);
        }

        [TestMethod]
        public async Task ThePortalTypeSaysHeroOrNormalAndNothingAboutTheCaptureSPlayerAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99),
                Captured.Arrival(132, 79, 108),
                Captured.Waypoint(104, 55, (WpPortalType)12, 81, 99)));

            Assert.AreEqual(2, _saved.Count);
            Assert.IsFalse(_saved.Single(s => s.MapId == 1).IsHeroic);
            Assert.IsTrue(_saved.Single(s => s.MapId == 132).IsHeroic);
        }

        [TestMethod]
        public async Task OneTimeSpaceReachedFromTwoMapsGivesTwoEntrancesAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(132, 79, 108),
                Captured.Waypoint(104, 55, (WpPortalType)12, 81, 99),
                Captured.Arrival(133, 79, 108),
                Captured.Waypoint(36, 55, (WpPortalType)12, 81, 99)));

            Assert.AreEqual(2, _saved.Count);
        }

        [TestMethod]
        public async Task ARaidPortalBecomesARaidEntranceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(2500, 79, 108),
                Captured.Portal(24, 3, 4996, PortalType.Raid)));

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(ScriptedInstanceType.Raid, _saved[0].Type);
            Assert.AreEqual(2500, _saved[0].MapId);
        }

        [TestMethod]
        public async Task AKnownRaidGateIsGivenItsRaidScriptAsync()
        {
            _existing.Add(new ScriptedInstanceDto
            {
                ScriptedInstanceId = 7, MapId = 2520, PositionX = 25, PositionY = 1, Type = ScriptedInstanceType.Raid
            });

            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(2520, 24, 47),
                Captured.Portal(25, 1, 4996, PortalType.Raid)));

            var gate = _saved.Single();
            Assert.AreEqual(7, gate.ScriptedInstanceId);
            Assert.AreEqual("raid-ginseng", gate.Script);
        }

        [TestMethod]
        public async Task AnUnknownRaidGateKeepsTheScriptItHadAsync()
        {
            _existing.Add(new ScriptedInstanceDto
            {
                ScriptedInstanceId = 7, MapId = 2500, PositionX = 24, PositionY = 3, Type = ScriptedInstanceType.Raid,
                Script = "my-raid"
            });

            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(2500, 24, 43),
                Captured.Portal(24, 3, 4996, PortalType.Raid)));

            Assert.AreEqual("my-raid", _saved.Single().Script);
        }

        [TestMethod]
        public async Task AnOrdinaryPortalIsNotAnEntranceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(2, 79, 108),
                Captured.Portal(24, 3, 1, PortalType.MapPortal),
                Captured.Portal(25, 3, 1, PortalType.Miniland)));

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task TheSameEntranceAnnouncedOnEveryVisitIsStoredOnceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99),
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99)));

            Assert.AreEqual(1, _saved.Count);
        }

        [TestMethod]
        public async Task AnEntranceAlreadyStoredGetsItsMetadataRefreshedAsync()
        {
            _existing.Add(new ScriptedInstanceDto
            {
                ScriptedInstanceId = 7,
                MapId = 1,
                PositionX = 134,
                PositionY = 36,
                Script = "<Definition>...</Definition>"
            });

            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99)));

            var refreshed = _saved.Single();
            Assert.AreEqual(1, refreshed.LevelMinimum);
            Assert.AreEqual(99, refreshed.LevelMaximum);

            Assert.AreEqual(7, refreshed.ScriptedInstanceId);
            Assert.AreEqual("<Definition>...</Definition>", refreshed.Script);
        }

        [TestMethod]
        public async Task OneEntranceSeenTwiceInTheCaptureIsSavedOnceAsync()
        {
            // The capture walks a map more than once, so the same wp comes round again.
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99)));

            Assert.AreEqual(1, _saved.Count);
        }

        [TestMethod]
        public async Task AnEntranceOnAMapTheServerDoesNotHaveIsDroppedAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(9999, 79, 108),
                Captured.Waypoint(134, 36, (WpPortalType)4, 1, 99)));

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ATimeSpaceEntranceRunsTheScriptNamedAfterItsTimeSpaceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(121, 26, (WpPortalType)4, 31, 99, 21)));

            Assert.AreEqual("timespace-21", _saved.Single().Script);
        }

        [TestMethod]
        public async Task AnEntranceStoredWithoutAScriptGetsItsTimeSpaceScriptAsync()
        {
            _existing.Add(new ScriptedInstanceDto { ScriptedInstanceId = 7, MapId = 1, PositionX = 121, PositionY = 26 });

            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(1, 79, 108),
                Captured.Waypoint(121, 26, (WpPortalType)4, 31, 99, 21)));

            Assert.AreEqual("timespace-21", _saved.Single().Script);
        }

        [TestMethod]
        public async Task ARaidEntranceGetsNoTimeSpaceScriptAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                Captured.Arrival(2500, 79, 108),
                Captured.Portal(24, 3, 4996, PortalType.Raid)));

            Assert.IsNull(_saved.Single().Script);
        }
    }
}
