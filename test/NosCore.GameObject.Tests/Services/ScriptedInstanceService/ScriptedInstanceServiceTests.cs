//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using NosCore.Tests.Shared;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Instances.Abstractions;
using System.Threading.Tasks;
using System;
using System.Linq;
using ServiceUnderTest = NosCore.GameObject.Services.ScriptedInstanceService.ScriptedInstanceService;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceServiceTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        private ServiceUnderTest Build(params ScriptedInstanceDto[] entrances)
        {
            return Create(entrances.ToList(), [], new Mock<IMapInstanceGeneratorService>(),
                new Mock<IInstanceScriptLoader>());
        }

        private ServiceUnderTest Create(List<ScriptedInstanceDto> entrances, List<MapDto> maps,
            Mock<IMapInstanceGeneratorService> generator, Mock<IInstanceScriptLoader> loader,
            List<NpcMonsterDto>? npcMonsters = null)
        {
            World.Inject(entrances);
            World.Inject(maps);
            World.Inject(npcMonsters ?? new List<NpcMonsterDto>());
            World.Inject(new System.Lazy<IMapInstanceGeneratorService>(generator.Object));
            World.Inject(new System.Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object));
            World.Inject(loader.Object);
            return World.Create<ServiceUnderTest>();
        }

        private static ScriptedInstanceDto TimeSpace(short id, short mapId, short x, short y,
            bool heroic = false, byte levelMinimum = 1)
        {
            return new ScriptedInstanceDto
            {
                ScriptedInstanceId = id,
                MapId = mapId,
                PositionX = x,
                PositionY = y,
                Type = ScriptedInstanceType.TimeSpace,
                IsHeroic = heroic,
                LevelMinimum = levelMinimum,
                LevelMaximum = 99
            };
        }

        [TestMethod]
        public void StandingNextToTheStoneFindsIt()
        {
            var service = Build(TimeSpace(1, 1, 10, 10), TimeSpace(2, 1, 14, 10));

            Assert.AreEqual(1, service.GetNear(1, 11, 9).NotNull().ScriptedInstanceId);
            Assert.AreEqual(2, service.GetNear(1, 14, 10).NotNull().ScriptedInstanceId);
            Assert.IsNull(service.GetNear(1, 12, 10));
        }

        [TestMethod]
        public void AMapWithNoEntrancesAnswersEmptyRatherThanThrowing()
        {
            var service = Build(TimeSpace(1, 1, 10, 10));

            Assert.AreEqual(0, service.GetByMap(2).Count);
            Assert.AreEqual(0, service.GenerateWp(2).Count());
            Assert.IsNull(service.GetAt(2, 10, 10));
        }

        [TestMethod]
        public void AnEntranceIsFoundByTheSquareItStandsOn()
        {
            var service = Build(TimeSpace(1, 1, 10, 20), TimeSpace(2, 1, 30, 40));

            Assert.AreEqual(2, service.GetAt(1, 30, 40).NotNull().ScriptedInstanceId);
            Assert.IsNull(service.GetAt(1, 30, 41));
        }

        [TestMethod]
        public void TheMarkerCarriesTheRowKeyTheClientWillSendBack()
        {
            var packet = Build(TimeSpace(42, 1, 10, 20, levelMinimum: 55)).GenerateWp(1).Single();

            Assert.AreEqual(42, packet.ScriptedInstanceId);
            Assert.AreEqual(10, packet.PositionX);
            Assert.AreEqual(20, packet.PositionY);
            Assert.AreEqual(55, packet.LevelMinimum);
            Assert.AreEqual(99, packet.LevelMaximum);
        }

        [TestMethod]
        public void AHeroTimeSpaceIsMarkedAsOne()
        {
            Assert.AreEqual(WpPortalType.HeroTs,
                Build(TimeSpace(1, 1, 10, 20, heroic: true)).GenerateWp(1).Single().PortalType);
            Assert.AreEqual(WpPortalType.NormalTs,
                Build(TimeSpace(1, 1, 10, 20)).GenerateWp(1).Single().PortalType);
        }

        [TestMethod]
        public void NoMarkerEverClaimsThePlayerHasClearedIt()
        {
            var service = Build(
                TimeSpace(1, 1, 10, 20),
                TimeSpace(2, 1, 30, 40, heroic: true));

            Assert.IsFalse(service.GenerateWp(1)
                .Any(s => s.PortalType is WpPortalType.NormalTsDone or WpPortalType.HeroTsDone));
        }

        [TestMethod]
        public void ARaidEntranceIsNotAMinimapMarker()
        {
            var service = Build(
                TimeSpace(1, 2500, 10, 20),
                new ScriptedInstanceDto
                {
                    ScriptedInstanceId = 2,
                    MapId = 2500,
                    PositionX = 24,
                    PositionY = 3,
                    Type = ScriptedInstanceType.Raid
                });

            Assert.AreEqual(2, service.GetByMap(2500).Count);
            Assert.AreEqual(1, service.GenerateWp(2500).Single().ScriptedInstanceId);
        }

        private ScriptedInstanceDto Scripted(short id, short mapId, short x, short y, string? script,
            ScriptedInstanceType type = ScriptedInstanceType.TimeSpace)
        {
            return new ScriptedInstanceDto
            {
                ScriptedInstanceId = id, MapId = mapId, PositionX = x, PositionY = y,
                Type = type, Script = script
            };
        }

        private (ServiceUnderTest Service, Mock<IMapInstanceGeneratorService> Generator, List<Guid> Removed)
            BuildWithMaps(ScriptedInstanceDto entrance, params short[] existingMapVNums) =>
            BuildWithMaps(entrance, [], existingMapVNums);

        private (ServiceUnderTest Service, Mock<IMapInstanceGeneratorService> Generator, List<Guid> Removed)
            BuildWithMaps(ScriptedInstanceDto entrance, List<NpcMonsterDto> npcMonsters, params short[] existingMapVNums)
        {
            var removed = new List<Guid>();
            var generator = new Mock<IMapInstanceGeneratorService>();
            generator
                .Setup(s => s.CreateMapInstance(It.IsAny<GameObject.Map.Map>(), It.IsAny<Guid>(), It.IsAny<bool>(),
                    It.IsAny<NosCore.Data.Enumerations.Map.MapInstanceType>()))
                .Returns((GameObject.Map.Map map, Guid id, bool shop, NosCore.Data.Enumerations.Map.MapInstanceType type)
                    => World.MapInstanceGeneratorService.CreateMapInstance(map, id, shop, type));
            generator.Setup(s => s.AddMapInstanceAsync(It.IsAny<GameObject.Services.MapInstanceGenerationService.MapInstance>()))
                .Returns(Task.CompletedTask);
            generator.Setup(s => s.RemoveMapAsync(It.IsAny<Guid>()))
                .Callback<Guid>(removed.Add).Returns(Task.CompletedTask);

            var maps = existingMapVNums.Select(v => new MapDto
            {
                MapId = v,
                NameI18NKey = "instanceRoom",
                Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
            }).ToList();
            var loader = new Mock<IInstanceScriptLoader>();
            loader.Setup(s => s.LoadAsync(TwoRooms)).ReturnsAsync(new TwoRoomScript());
            loader.Setup(s => s.LoadAsync(Broken)).ThrowsAsync(new InvalidOperationException("did not compile"));
            loader.Setup(s => s.LoadAsync(MapRooms)).ReturnsAsync(new MapRoomScript());
            return (Create([entrance], maps, generator, loader, npcMonsters), generator, removed);
        }

        private const string TwoRooms = "two-rooms";

        private const string Broken = "broken";

        private const string MapRooms = "map-rooms";

        private sealed class MapRoomScript : InstanceScript
        {
            public MapRoomScript()
            {
                Definition = InstanceDefinitionBuilder.Named(1, "map-rooms", "map-rooms")
                    .WithMapRoom(2004, out _)
                    .WithRoom(2004, out _)
                    .Build();
            }
        }

        [TestMethod]
        public async Task AMapRoomComesWithItsMapsMonstersAsync()
        {
            await World.MapMonsterDao.TryInsertOrUpdateAsync(new MapMonsterDto { MapMonsterId = 1, MapId = 2004, VNum = 58, MapX = 1, MapY = 1 });
            await World.MapMonsterDao.TryInsertOrUpdateAsync(new MapMonsterDto { MapMonsterId = 2, MapId = 2005, VNum = 58, MapX = 1, MapY = 1 });
            var (service, generator, _) = BuildWithMaps(Scripted(1, 1, 5, 6, MapRooms),
                [new NpcMonsterDto { NpcMonsterVNum = 58, MaxHp = 10 }], 2004);
            var built = new List<GameObject.Services.MapInstanceGenerationService.MapInstance>();
            generator.Setup(s => s.AddMapInstanceAsync(It.IsAny<GameObject.Services.MapInstanceGenerationService.MapInstance>()))
                .Callback<GameObject.Services.MapInstanceGenerationService.MapInstance>(built.Add).Returns(Task.CompletedTask);

            var run = (await service.InstantiateAsync(service.GetAt(1, 5, 6).NotNull())).NotNull();

            Assert.AreEqual(1, built.Single(m => m.MapInstanceId == run.Rooms[1]).EnumerateMonsters().Count());
            Assert.AreEqual(0, built.Single(m => m.MapInstanceId == run.Rooms[2]).EnumerateMonsters().Count());
        }

        private sealed class TwoRoomScript : InstanceScript
        {
            public TwoRoomScript()
            {
                Definition = InstanceDefinitionBuilder.Named(1, "two-rooms", "two-rooms")
                    .StartingAt(3, 4)
                    .WithRoom(2004, out _)
                    .WithRoom(2005, out _, 1)
                    .Build();
            }
        }

        [TestMethod]
        public async Task EachRoomOfTheDefinitionBecomesItsOwnMapInstanceAsync()
        {
            var (service, _, _) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);

            var run = await service.InstantiateAsync(service.GetAt(1, 5, 6).NotNull());

            Assert.IsNotNull(run);
            var rooms = run.Rooms;
            Assert.AreEqual(2, rooms.Count);
            Assert.IsTrue(rooms.ContainsKey(1));
            Assert.IsTrue(rooms.ContainsKey(2));
            Assert.AreNotEqual(rooms[1], rooms[2]);
        }

        [TestMethod]
        public async Task TwoPartiesEnteringTheSameDoorDoNotMeetAsync()
        {
            var (service, _, _) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);
            var entrance = service.GetAt(1, 5, 6).NotNull();

            var first = (await service.InstantiateAsync(entrance)).NotNull();
            var second = (await service.InstantiateAsync(entrance)).NotNull();

            Assert.AreNotEqual(first.Rooms[1], second.Rooms[1]);
        }

        [TestMethod]
        public async Task ADoorWithNoScriptBuildsNothingAsync()
        {
            var (service, generator, _) = BuildWithMaps(Scripted(1, 1, 5, 6, null));

            Assert.IsNull(await service.InstantiateAsync(service.GetAt(1, 5, 6).NotNull()));
            generator.Verify(s => s.AddMapInstanceAsync(It.IsAny<GameObject.Services.MapInstanceGenerationService.MapInstance>()),
                Times.Never);
        }

        [TestMethod]
        public async Task AScriptThatDoesNotCompileBuildsNothingAsync()
        {
            var (service, generator, _) = BuildWithMaps(Scripted(1, 1, 5, 6, Broken), 2004, 2005);

            Assert.IsNull(await service.InstantiateAsync((service.GetAt(1, 5, 6) ?? throw new AssertFailedException("no entrance"))));
            generator.Verify(s => s.AddMapInstanceAsync(It.IsAny<GameObject.Services.MapInstanceGenerationService.MapInstance>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TheScriptIsWhatTheTimeSpaceWindowDescribesAsync()
        {
            var (service, _, _) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);
            var entrance = (service.GetAt(1, 5, 6) ?? throw new AssertFailedException("no entrance"));

            Assert.IsNull(entrance.Definition);
            Assert.IsTrue(await service.LoadAsync(entrance));
            Assert.AreEqual("two-rooms", entrance.GenerateRbr().TitleAndDescription?.Title);
        }

        [TestMethod]
        public async Task AMissingRoomUndoesTheRoomsAlreadyBuiltAsync()
        {
            var (service, _, removed) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004);

            var run = await service.InstantiateAsync(service.GetAt(1, 5, 6).NotNull());

            Assert.IsNull(run);
            Assert.AreEqual(1, removed.Count, "the room that was built before the failure has to be torn down");
        }

        [TestMethod]
        public async Task TearingDownRemovesEveryRoomAsync()
        {
            var (service, _, removed) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);
            var run = (await service.InstantiateAsync(service.GetAt(1, 5, 6).NotNull())).NotNull();

            Assert.IsTrue(await service.DisposeIfEmptyAsync(run));
            Assert.AreEqual(2, removed.Count);
        }
    }
}
