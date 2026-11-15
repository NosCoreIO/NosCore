//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class CapturedMapRerunTests
    {
        private static List<IPacket> Capture() =>
        [
            Captured.Arrival(1),
            Captured.Monster(1, 7, 10, 10),
            Captured.Portal(5, 5, 2, PortalType.MapPortal),
            Captured.Arrival(2),
            Captured.Portal(8, 8, 1, PortalType.MapPortal),
        ];

        private sealed class Database
        {
            private static IDao<TDto, TPk> Dao<TEntity, TDto, TPk>(Func<NosCoreContext> context) where TEntity : class where TPk : struct =>
                new Dao<TEntity, TDto, TPk>(NullLogger<Dao<TEntity, TDto, TPk>>.Instance, context);

            public Database()
            {
                var options = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                NosCoreContext Context() => new(options);
                Monsters = Dao<MapMonster, MapMonsterDto, int>(Context);
                Portals = Dao<Portal, PortalDto, int>(Context);
            }

            public IDao<MapMonsterDto, int> Monsters { get; }
            public IDao<PortalDto, int> Portals { get; }

            public async Task ImportAsync()
            {
                var log = new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object;
                var npcMonsters = new Mock<IDao<NpcMonsterDto, short>>();
                npcMonsters.Setup(s => s.LoadAll()).Returns([new NpcMonsterDto { NpcMonsterVNum = 1 }]);
                var maps = new Mock<IDao<MapDto, short>>();
                maps.Setup(s => s.LoadAll()).Returns(new short[] { 1, 2, 3, 98, 145, 150 }.Select(m => new MapDto { MapId = m }).ToList());
                var packets = Capture();
                CaptureSessions.MergeRepeatedEntities(packets);

                await new MapMonsterParser(Monsters, npcMonsters.Object, new Mock<ILogger<MapMonsterParser>>().Object, log).InsertMapMonsterAsync(packets);
                await new PortalParser(new Mock<ILogger<PortalParser>>().Object, maps.Object, Portals, log).InsertPortalsAsync(packets);
            }

            public string Contents() => string.Join("\n", new[]
            {
                Monsters.LoadAll().Select(s => $"monster {s.MapMonsterId} {s.MapId} {s.VNum} {s.MapX} {s.MapY}"),
                Portals.LoadAll().Select(s => $"portal {s.SourceMapId} {s.SourceX} {s.SourceY} {s.DestinationMapId} {s.DestinationX} {s.DestinationY} {s.Type}"),
            }.SelectMany(s => s).Order());
        }

        private static async Task<Database> AnEarlierImportAsync()
        {
            var database = new Database();
            await database.Monsters.TryInsertOrUpdateAsync(new[]
            {
                new MapMonsterDto { MapMonsterId = 7, MapId = 1, VNum = 1, MapX = 12, MapY = 12 },
                new MapMonsterDto { MapMonsterId = 8, MapId = 1, VNum = 1, MapX = 30, MapY = 30 },
                new MapMonsterDto { MapMonsterId = 9, MapId = 3, VNum = 1, MapX = 30, MapY = 30 },
            });
            await database.Portals.TryInsertOrUpdateAsync(new[]
            {
                new PortalDto { SourceMapId = 1, SourceX = 5, SourceY = 5, DestinationMapId = 2, DestinationX = 1, DestinationY = 1, Type = PortalType.MapPortal },
                new PortalDto { SourceMapId = 1, SourceX = 40, SourceY = 40, DestinationMapId = 3, DestinationX = 1, DestinationY = 1, Type = PortalType.MapPortal },
                new PortalDto { SourceMapId = 150, SourceX = 172, SourceY = 171, DestinationMapId = 98, DestinationX = 6, DestinationY = 36, Type = PortalType.MapPortal },
                new PortalDto { SourceMapId = 150, SourceX = 172, SourceY = 171, DestinationMapId = 98, DestinationX = 6, DestinationY = 36, Type = PortalType.MapPortal },
                new PortalDto { SourceMapId = 3, SourceX = 9, SourceY = 9, DestinationMapId = 1, DestinationX = 40, DestinationY = 40, Type = PortalType.MapPortal },
            });
            return database;
        }

        [TestMethod]
        public async Task RerunningOnAnEarlierImportKeepsOnlyWhatTheCaptureShowsOnItsMaps()
        {
            var fresh = new Database();
            await fresh.ImportAsync();
            var rerun = await AnEarlierImportAsync();

            await rerun.ImportAsync();

            StringAssert.Contains(fresh.Contents(), "portal 1 5 5 2 8 8 MapPortal");
            StringAssert.Contains(rerun.Contents(), "monster 9 3 1 30 30");
            StringAssert.Contains(rerun.Contents(), "portal 3 9 9 1 40 40 MapPortal");
            Assert.AreEqual(fresh.Contents(), string.Join("\n", rerun.Contents().Split('\n')
                .Where(l => l is not ("monster 9 3 1 30 30" or "portal 3 9 9 1 40 40 MapPortal"))));
        }

        [TestMethod]
        public async Task ASecondRunChangesNothing()
        {
            var database = new Database();
            await database.ImportAsync();
            var first = database.Contents();
            var ids = database.Portals.LoadAll().Select(s => s.PortalId).Order().ToList();

            await database.ImportAsync();

            Assert.AreEqual(first, database.Contents());
            CollectionAssert.AreEqual(ids, database.Portals.LoadAll().Select(s => s.PortalId).Order().ToList());
        }
    }
}
