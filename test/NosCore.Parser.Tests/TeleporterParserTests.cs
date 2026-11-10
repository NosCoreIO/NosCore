//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using NosCore.Packets.Enumerations;
using SpecLight;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class TeleporterParserTests
    {
        private Mock<IDao<TeleporterDto, short>> _teleporterDao = null!;
        private List<TeleporterDto> _existing = null!;
        private List<TeleporterDto> _saved = null!;
        private TeleporterParser _parser = null!;

        [TestInitialize]
        public void Setup()
        {
            _existing = [];
            _saved = [];
            _teleporterDao = new Mock<IDao<TeleporterDto, short>>();
            _teleporterDao.Setup(x => x.LoadAll()).Returns(() => _existing);
            _teleporterDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<TeleporterDto>>()))
                .Callback<IEnumerable<TeleporterDto>>(t => _saved.AddRange(t))
                .ReturnsAsync(true);
            var mapNpcDao = new Mock<IDao<MapNpcDto, int>>();
            mapNpcDao.Setup(x => x.LoadAll()).Returns(new List<MapNpcDto>
            {
                new() { MapNpcId = 1899, MapId = 1, VNum = 330, Dialog = 11 },
                new() { MapNpcId = 2022, MapId = 1, VNum = 330, Dialog = 11 },
                new() { MapNpcId = 9821, MapId = 177, VNum = 955, Dialog = 453 },
            });
            _parser = new TeleporterParser(_teleporterDao.Object, mapNpcDao.Object,
                new Mock<ILogger<TeleporterParser>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        private static string[] P(string line) => line.Split(' ');

        [TestMethod]
        public async Task GlacernonReturnRecordsThePortDestination()
        {
            await _parser.InsertTeleportersAsync([
                P("n_run 5002 0 2 1899"), P("at 1 145 51 41 2 0 0 1 -1")
            ]);
            var destination = _saved.Single(s => s.MapNpcId == 1899);
            Assert.AreEqual(NrunRunnerType.ReturnFromGlacernon, destination.Runner);
            Assert.AreEqual((short)145, destination.MapId);
            Assert.AreEqual((short)51, destination.MapX);
            Assert.AreEqual((short)41, destination.MapY);
        }

        [TestMethod]
        public async Task TheArrivalAfterATeleportIsTheDestination()
        {
            await _parser.InsertTeleportersAsync([
                P("n_run 16 1 2 1899"),
                P("c_close"),
                P("at 1 20 8 87 2 0 0 1 -1"),
            ]);

            var teleporter = _saved.Single(s => s.MapNpcId == 1899);
            Assert.AreEqual(1, teleporter.Index);
            Assert.AreEqual(20, teleporter.MapId);
            Assert.AreEqual(8, teleporter.MapX);
            Assert.AreEqual(87, teleporter.MapY);
            Assert.AreEqual(TeleporterType.Teleporter, teleporter.Type);
        }

        [TestMethod]
        public async Task ADestinationOnTheSameMapIsMarkedSo()
        {
            await _parser.InsertTeleportersAsync([P("n_run 26 1 2 9821"), P("at 1 177 148 71 2 0 0 1 -1")]);

            Assert.AreEqual(TeleporterType.TeleporterOnMap, _saved.Single().Type);
        }

        [TestMethod]
        public async Task OtherRunnersAndUnknownNpcsAreIgnored()
        {
            await _parser.InsertTeleportersAsync([
                P("n_run 17 0 2 1899"), P("at 1 2006 20 20 2 0 0 1 -1"),
                P("n_run 16 0 2 5555"), P("at 1 1 80 110 2 0 0 1 -1"),
            ]);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task OnlyTheFirstArrivalCounts()
        {
            await _parser.InsertTeleportersAsync([
                P("n_run 16 1 2 1899"), P("at 1 20 8 87 2 0 0 1 -1"),
                P("n_run 16 1 2 1899"), P("at 1 20 9 90 2 0 0 1 -1"),
            ]);

            Assert.AreEqual(8, _saved.Single(s => s.MapNpcId == 1899).MapX);
        }

        [TestMethod]
        public async Task AnAtWithoutATeleportBeforeItIsNotADestination()
        {
            await _parser.InsertTeleportersAsync([P("at 1 20 8 87 2 0 0 1 -1"), P("n_run 16 1 2 1899")]);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task CopiesOfTheSameNpcShareItsDestinations()
        {
            await _parser.InsertTeleportersAsync([P("n_run 16 1 2 1899"), P("at 1 20 8 87 2 0 0 1 -1")]);

            var copy = _saved.Single(s => s.MapNpcId == 2022);
            Assert.AreEqual(1, copy.Index);
            Assert.AreEqual(20, copy.MapId);
        }

        [TestMethod]
        public async Task TeleportersAlreadyStoredAreKept()
        {
            _existing.Add(new TeleporterDto { MapNpcId = 1899, Runner = NrunRunnerType.Teleport, Index = 1, MapId = 20, MapX = 1, MapY = 1 });

            await _parser.InsertTeleportersAsync([P("n_run 16 1 2 1899"), P("at 1 20 8 87 2 0 0 1 -1")]);

            Assert.IsFalse(_saved.Any(s => s.MapNpcId == 1899));
        }

        [TestMethod]
        public async Task TheMenuOfADestinationIsStored()
        {
            await new Spec("A destination is stored with the runner of the menu it was picked from")
                .WhenAsync(TheTraceIs_, new[] { "n_run 26 1 2 9821", "at 1 177 148 71 2 0 0 1 -1" })
                .Then(TheDestinationOf_Option_ShouldBeUnder_, 9821, (short)1, NrunRunnerType.TeleportActV)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameOptionOfTwoMenusIsKeptApart()
        {
            await new Spec("The same option index under two menus of one NPC gives two destinations")
                .WhenAsync(TheTraceIs_, new[]
                {
                    "n_run 16 1 2 9821", "at 1 20 8 87 2 0 0 1 -1",
                    "n_run 26 1 2 9821", "at 1 177 148 71 2 0 0 1 -1",
                })
                .Then(TheDestinationOf_Option_ShouldBeUnder_, 9821, (short)1, NrunRunnerType.Teleport)
                .And(TheDestinationOf_Option_ShouldBeUnder_, 9821, (short)1, NrunRunnerType.TeleportActV)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStoredDestinationWithoutAMenuTakesTheCapturedOne()
        {
            await new Spec("A destination stored without its menu is given the captured runner instead of a duplicate")
                .Given(ADestinationWithoutAMenuIsStoredFor_Option_, 1899, (short)1)
                .WhenAsync(TheTraceIs_, new[] { "n_run 16 1 2 1899", "at 1 20 8 87 2 0 0 1 -1" })
                .Then(TheStoredDestinationShouldNowBeUnder_, NrunRunnerType.Teleport)
                .And(DestinationsSavedFor_ShouldBe_, 1899, 1)
                .ExecuteAsync();
        }

        private const short StoredTeleporterId = 7;

        private void ADestinationWithoutAMenuIsStoredFor_Option_(int npcId, short index) =>
            _existing.Add(new TeleporterDto
            {
                TeleporterId = StoredTeleporterId, MapNpcId = npcId, Index = index, MapId = 20, MapX = 8, MapY = 87
            });

        private Task TheTraceIs_(string[] lines) => _parser.InsertTeleportersAsync(lines.Select(P).ToList());

        private void TheDestinationOf_Option_ShouldBeUnder_(int npcId, short index, NrunRunnerType runner) =>
            Assert.AreEqual(1, _saved.Count(s => s.MapNpcId == npcId && s.Index == index && s.Runner == runner));

        private void TheStoredDestinationShouldNowBeUnder_(NrunRunnerType runner) =>
            Assert.AreEqual(runner, _saved.Single(s => s.TeleporterId == StoredTeleporterId).Runner);

        private void DestinationsSavedFor_ShouldBe_(int npcId, int count) =>
            Assert.AreEqual(count, _saved.Count(s => s.MapNpcId == npcId));
    }
}
