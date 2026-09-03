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
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using NosCore.Tests.Shared;
using System.Threading.Tasks;
using System;
using System.Linq;
using ServiceUnderTest = NosCore.GameObject.Services.ScriptedInstanceService.ScriptedInstanceService;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class ScriptedInstanceServiceTests
    {
        [TestInitialize]
        public Task SetupAsync()
        {
            return TestHelpers.ResetAsync();
        }

        private static ServiceUnderTest Build(params ScriptedInstanceDto[] entrances)
        {
            return new ServiceUnderTest(entrances.ToList(), [], new Mock<IMapInstanceGeneratorService>().Object, new MapInstanceRegistry(), TestHelpers.Instance.Clock, NullLogger<ServiceUnderTest>.Instance);
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

            Assert.AreEqual(2, service.GetAt(1, 30, 40)!.ScriptedInstanceId);
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

        private static ScriptedInstanceDto Scripted(short id, short mapId, short x, short y, string script,
            ScriptedInstanceType type = ScriptedInstanceType.TimeSpace)
        {
            return new ScriptedInstanceDto
            {
                ScriptedInstanceId = id, MapId = mapId, PositionX = x, PositionY = y,
                Type = type, Script = script
            };
        }

        private static (ServiceUnderTest Service, Mock<IMapInstanceGeneratorService> Generator, List<Guid> Removed)
            BuildWithMaps(ScriptedInstanceDto entrance, params short[] existingMapVNums)
        {
            var removed = new List<Guid>();
            var generator = new Mock<IMapInstanceGeneratorService>();
            generator
                .Setup(s => s.CreateMapInstance(It.IsAny<GameObject.Map.Map>(), It.IsAny<Guid>(), It.IsAny<bool>(),
                    It.IsAny<NosCore.Data.Enumerations.Map.MapInstanceType>()))
                .Returns((GameObject.Map.Map map, Guid id, bool shop, NosCore.Data.Enumerations.Map.MapInstanceType type)
                    => TestHelpers.Instance.MapInstanceGeneratorService.CreateMapInstance(map, id, shop, type));
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
            return (new ServiceUnderTest([entrance], maps, generator.Object,
                    TestHelpers.Instance.MapInstanceRegistry, TestHelpers.Instance.Clock,
                    NullLogger<ServiceUnderTest>.Instance),
                generator, removed);
        }

        private const string TwoRooms = """
            <Definition>
              <Globals><StartX Value="3" /><StartY Value="4" /></Globals>
              <InstanceEvents>
                <CreateMap Map="1" VNum="2004" IndexX="0" IndexY="0" />
                <CreateMap Map="2" VNum="2005" IndexX="1" IndexY="0" />
              </InstanceEvents>
            </Definition>
            """;

        [TestMethod]
        public async Task EachRoomOfTheDefinitionBecomesItsOwnMapInstanceAsync()
        {
            var (service, _, _) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);

            var run = await service.InstantiateAsync(service.GetAt(1, 5, 6)!);

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
            var entrance = service.GetAt(1, 5, 6)!;

            var first = await service.InstantiateAsync(entrance);
            var second = await service.InstantiateAsync(entrance);

            Assert.AreNotEqual(first!.Rooms[1], second!.Rooms[1]);
        }

        [TestMethod]
        public async Task ADoorWithNoScriptBuildsNothingAsync()
        {
            var (service, generator, _) = BuildWithMaps(Scripted(1, 1, 5, 6, null!));

            Assert.IsNull(await service.InstantiateAsync(service.GetAt(1, 5, 6)!));
            generator.Verify(s => s.AddMapInstanceAsync(It.IsAny<GameObject.Services.MapInstanceGenerationService.MapInstance>()),
                Times.Never);
        }

        [TestMethod]
        public async Task AMissingRoomUndoesTheRoomsAlreadyBuiltAsync()
        {
            var (service, _, removed) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004);

            var run = await service.InstantiateAsync(service.GetAt(1, 5, 6)!);

            Assert.IsNull(run);
            Assert.AreEqual(1, removed.Count, "the room that was built before the failure has to be torn down");
        }

        [TestMethod]
        public async Task TearingDownRemovesEveryRoomAsync()
        {
            var (service, _, removed) = BuildWithMaps(Scripted(1, 1, 5, 6, TwoRooms), 2004, 2005);
            var run = await service.InstantiateAsync(service.GetAt(1, 5, 6)!);

            Assert.IsTrue(await service.DisposeIfEmptyAsync(run!));
            Assert.AreEqual(2, removed.Count);
        }
    }
}
