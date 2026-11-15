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
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using SpecLight;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class TeleporterParserTests
    {
        private Mock<IDao<TeleporterDto, short>> _teleporterDao;
        private List<TeleporterDto> _existing;
        private List<TeleporterDto> _saved;
        private TeleporterParser _parser;

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

        [TestMethod]
        public async Task TheArrivalAfterATeleportIsTheDestination()
        {
            await _parser.InsertTeleportersAsync([
                Captured.Talk(NrunRunnerType.Teleport, 1, 1899),
                new CClosePacket(),
                Captured.Arrival(20, 8, 87),
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
            await _parser.InsertTeleportersAsync([Captured.Talk(NrunRunnerType.TeleportActV, 1, 9821), Captured.Arrival(177, 148, 71)]);

            Assert.AreEqual(TeleporterType.TeleporterOnMap, _saved.Single().Type);
        }

        [TestMethod]
        public async Task OtherRunnersAndUnknownNpcsAreIgnored()
        {
            await _parser.InsertTeleportersAsync([
                Captured.Talk(NrunRunnerType.GoToArena, 0, 1899), Captured.Arrival(2006, 20, 20),
                Captured.Talk(NrunRunnerType.Teleport, 0, 5555), Captured.Arrival(1, 80, 110),
            ]);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task OnlyTheFirstArrivalCounts()
        {
            await _parser.InsertTeleportersAsync([
                Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87),
                Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 9, 90),
            ]);

            Assert.AreEqual(8, _saved.Single(s => s.MapNpcId == 1899).MapX);
        }

        [TestMethod]
        public async Task AnAtWithoutATeleportBeforeItIsNotADestination()
        {
            await _parser.InsertTeleportersAsync([Captured.Arrival(20, 8, 87), Captured.Talk(NrunRunnerType.Teleport, 1, 1899)]);

            Assert.AreEqual(0, _saved.Count);
        }

        [TestMethod]
        public async Task CopiesOfTheSameNpcShareItsDestinations()
        {
            await _parser.InsertTeleportersAsync([Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87)]);

            var copy = _saved.Single(s => s.MapNpcId == 2022);
            Assert.AreEqual(1, copy.Index);
            Assert.AreEqual(20, copy.MapId);
        }

        [TestMethod]
        public async Task AStoredTeleporterTakesTheCapturedDestinationInPlace()
        {
            _existing.Add(new TeleporterDto { TeleporterId = 7, MapNpcId = 1899, Runner = NrunRunnerType.Teleport, Index = 1, MapId = 20, MapX = 1, MapY = 1 });

            await _parser.InsertTeleportersAsync([Captured.Npc(330, 1899, 10, 10), Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87)]);

            var teleporter = _saved.Single(s => s.MapNpcId == 1899);
            Assert.AreEqual((short)7, teleporter.TeleporterId);
            Assert.AreEqual((short)8, teleporter.MapX);
            _teleporterDao.Verify(x => x.TryDeleteAsync(It.Is<IEnumerable<short>>(ids => !ids.Any())), Times.Once);
        }

        [TestMethod]
        public async Task AStoredDestinationTheCaptureDoesNotShowForItsNpcIsRemoved()
        {
            _existing.Add(new TeleporterDto { TeleporterId = 7, MapNpcId = 1899, Runner = NrunRunnerType.Teleport, Index = 3, MapId = 20, MapX = 1, MapY = 1 });

            await _parser.InsertTeleportersAsync([Captured.Npc(330, 1899, 10, 10), Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87)]);

            _teleporterDao.Verify(x => x.TryDeleteAsync(It.Is<IEnumerable<short>>(ids => ids.SequenceEqual(new short[] { 7 }))), Times.Once);
        }

        [TestMethod]
        public async Task AStoredDestinationOfAnNpcTheCaptureDoesNotShowIsKept()
        {
            _existing.Add(new TeleporterDto { TeleporterId = 7, MapNpcId = 9821, Runner = NrunRunnerType.Teleport, Index = 3, MapId = 20, MapX = 1, MapY = 1 });

            await _parser.InsertTeleportersAsync([Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87)]);

            _teleporterDao.Verify(x => x.TryDeleteAsync(It.Is<IEnumerable<short>>(ids => ids.Any())), Times.Never);
        }

        [TestMethod]
        public async Task TheMenuOfADestinationIsStored()
        {
            await new Spec("A destination is stored with the runner of the menu it was picked from")
                .WhenAsync(TheTraceIs_, new IPacket[] { Captured.Talk(NrunRunnerType.TeleportActV, 1, 9821), Captured.Arrival(177, 148, 71) })
                .Then(TheDestinationOf_Option_ShouldBeUnder_, 9821, (short)1, NrunRunnerType.TeleportActV)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSameOptionOfTwoMenusIsKeptApart()
        {
            await new Spec("The same option index under two menus of one NPC gives two destinations")
                .WhenAsync(TheTraceIs_, new IPacket[]
                {
                    Captured.Talk(NrunRunnerType.Teleport, 1, 9821), Captured.Arrival(20, 8, 87),
                    Captured.Talk(NrunRunnerType.TeleportActV, 1, 9821), Captured.Arrival(177, 148, 71),
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
                .WhenAsync(TheTraceIs_, new IPacket[] { Captured.Talk(NrunRunnerType.Teleport, 1, 1899), Captured.Arrival(20, 8, 87) })
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

        private Task TheTraceIs_(IPacket[] packets) => _parser.InsertTeleportersAsync(packets);

        private void TheDestinationOf_Option_ShouldBeUnder_(int npcId, short index, NrunRunnerType runner) =>
            Assert.AreEqual(1, _saved.Count(s => s.MapNpcId == npcId && s.Index == index && s.Runner == runner));

        private void TheStoredDestinationShouldNowBeUnder_(NrunRunnerType runner) =>
            Assert.AreEqual(runner, _saved.Single(s => s.TeleporterId == StoredTeleporterId).Runner);

        private void DestinationsSavedFor_ShouldBe_(int npcId, int count) =>
            Assert.AreEqual(count, _saved.Count(s => s.MapNpcId == npcId));
    }
}
