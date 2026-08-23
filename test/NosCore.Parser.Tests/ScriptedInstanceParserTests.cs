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
        private Mock<IDao<MapDto, short>> _mapDao = null!;
        private Mock<IDao<ScriptedInstanceDto, short>> _instanceDao = null!;
        private List<ScriptedInstanceDto> _saved = null!;
        private List<ScriptedInstanceDto> _existing = null!;
        private ScriptedInstanceParser _parser = null!;

        [TestInitialize]
        public void Setup()
        {
            _saved = [];
            _existing = [];

            _mapDao = new Mock<IDao<MapDto, short>>();
            _mapDao.Setup(s => s.LoadAll()).Returns(new List<MapDto>
            {
                new() { MapId = 1 }, new() { MapId = 2 }, new() { MapId = 132 },
                new() { MapId = 133 }, new() { MapId = 2500 }
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

        private static string[] Line(string packet) => packet.Split(' ');

        private static List<string[]> Capture(params string[] packets) => packets.Select(Line).ToList();

        [TestMethod]
        public async Task AWaypointBecomesATimeSpaceEntranceOnTheMapItFollowsAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99"));

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
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99",
                "at 1234 132 79 108 2 0 0 0",
                "wp 104 55 79 12 81 99"));

            Assert.AreEqual(2, _saved.Count);
            Assert.IsFalse(_saved.Single(s => s.MapId == 1).IsHeroic);
            Assert.IsTrue(_saved.Single(s => s.MapId == 132).IsHeroic);
        }

        [TestMethod]
        public async Task OneTimeSpaceReachedFromTwoMapsGivesTwoEntrancesAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 132 79 108 2 0 0 0",
                "wp 104 55 79 12 81 99",
                "at 1234 133 79 108 2 0 0 0",
                "wp 36 55 79 12 81 99"));

            Assert.AreEqual(2, _saved.Count);
        }

        [TestMethod]
        public async Task ARaidPortalBecomesARaidEntranceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 2500 79 108 2 0 0 0",
                "gp 24 3 4996 8 0 0"));

            Assert.AreEqual(1, _saved.Count);
            Assert.AreEqual(ScriptedInstanceType.Raid, _saved[0].Type);
            Assert.AreEqual(2500, _saved[0].MapId);
        }

        [TestMethod]
        public async Task AnOrdinaryPortalIsNotAnEntranceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 2 79 108 2 0 0 0",
                "gp 24 3 1 -1 0 0",
                "gp 25 3 1 3 0 0"));

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task TheSameEntranceAnnouncedOnEveryVisitIsStoredOnceAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99",
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99"));

            Assert.AreEqual(1, _saved.Count);
        }

        [TestMethod]
        public async Task AnEntranceAlreadyStoredIsLeftAloneAsync()
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
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99"));

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task AnEntranceOnAMapTheServerDoesNotHaveIsDroppedAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 9999 79 108 2 0 0 0",
                "wp 134 36 0 4 1 99"));

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task ATruncatedLineDoesNotBringTheImportDownAsync()
        {
            await _parser.InsertScriptedInstancesAsync(Capture(
                "at 1234 1 79 108 2 0 0 0",
                "wp 134 36",
                "gp 24",
                "wp 134 36 0 4 1 99"));

            Assert.AreEqual(1, _saved.Count);
        }
    }
}
