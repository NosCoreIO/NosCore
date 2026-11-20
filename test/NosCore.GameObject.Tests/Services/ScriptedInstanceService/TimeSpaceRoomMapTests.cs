//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.TimeSpacePenaltyService;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class TimeSpaceRoomMapTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ClientSession _partner;
        private ScriptedInstanceRun _run;
        private readonly Dictionary<int, MapInstance> _rooms = [];
        private readonly Dictionary<int, Mock<ISessionGroup>> _roomSessions = [];

        private sealed class Script : InstanceScript
        {
            public Script(ScriptedInstanceDefinition definition) => Definition = definition;

            public override void OnFirstEnter(IInstanceRun run) => run.StartClock(TimeSpan.FromSeconds(60));
        }

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _partner = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task TheFirstRoomShowsEveryRoomAndOpensTheMap()
        {
            await new Spec("Entering a time-space shows every room, the one entered as found, marks the player there and opens the map")
                .Given(AThreeRoomTimeSpace)
                .WhenAsync(ThePlayerEntersRoom_, 1)
                .Then(ThePlayerShouldBeShownRooms_, "3.11.0 3.10.1 3.9.1")
                .And(ThePlayerShouldBeMarkedAt_, 3, 11)
                .And(TheMapShouldOpen_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFoundRoomIsFilledInForTheWholeRun()
        {
            await new Spec("Walking into a room nobody had found fills it in for the run and moves the marker, without reopening the map")
                .Given(AThreeRoomTimeSpace)
                .AndAsync(ThePlayerEntersRoom_, 1)
                .WhenAsync(ThePlayerEntersRoom_, 2)
                .Then(TheRunShouldBeShownRooms_, "3.10.0")
                .And(ThePlayerShouldBeMarkedAt_, 3, 10)
                .And(TheMapShouldOpen_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GoingBackToAFoundRoomOnlyMovesTheMarker()
        {
            await new Spec("Walking back into a room already found only moves the marker")
                .Given(AThreeRoomTimeSpace)
                .AndAsync(ThePlayerEntersRoom_, 1)
                .AndAsync(ThePlayerEntersRoom_, 2)
                .WhenAsync(ThePlayerEntersRoom_, 1)
                .Then(ThePlayerShouldBeShownRooms_, "")
                .And(TheRunShouldBeShownRooms_, "")
                .And(ThePlayerShouldBeMarkedAt_, 3, 11)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerArrivingLaterSeesWhatTheRunFound()
        {
            await new Spec("A party member entering after rooms were found sees those rooms as found and gets the map opened")
                .Given(AThreeRoomTimeSpace)
                .AndAsync(ThePlayerEntersRoom_, 1)
                .AndAsync(ThePlayerEntersRoom_, 2)
                .WhenAsync(ThePartnerEntersRoom_, 1)
                .Then(ThePartnerShouldBeShownRooms_, "3.11.0 3.10.0 3.9.1")
                .And(TheMapShouldOpenForThePartner)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OutsideARunTheMarkerIsCleared()
        {
            await new Spec("Entering a map that belongs to no run clears the room marker")
                .WhenAsync(ThePlayerEntersAnOrdinaryMap)
                .Then(ThePlayerShouldBeMarkedAt_, 0, -1)
                .ExecuteAsync();
        }

        private void AThreeRoomTimeSpace()
        {
            var definition = InstanceDefinitionBuilder.Named(1, "timespace", "timespace").WithLives(1)
                .WithRoom(2004, out var first, 3, 11)
                .WithRoom(2004, out var second, 3, 10)
                .WithRoom(2004, out var third, 3, 9)
                .Build();
            foreach (var key in new[] { first, second, third })
            {
                var room = World.MapInstanceGeneratorService.CreateMapInstance(
                    new GameObject.Map.Map
                    {
                        MapId = 2004,
                        NameI18NKey = "instanceRoom",
                        Data = [4, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
                    }, Guid.NewGuid(), false, MapInstanceType.TimeSpaceInstance);
                _roomSessions[key] = new Mock<ISessionGroup>();
                room.Sessions = _roomSessions[key].Object;
                _rooms[key] = room;
            }

            var script = new Script(definition);
            _run = new ScriptedInstanceRun(
                new TimeSpaceRunHost(
                    new GameObject.Services.ScriptedInstanceService.ScriptedInstance { ScriptedInstanceId = 1, MapId = 1, Logic = script },
                    new Lazy<IMapChangeService>(new Mock<IMapChangeService>().Object), new Mock<IGameEventDispatcher>().Object, World.Clock, World.Create<TimeSpacePenaltyService>()),
                script, _rooms, new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog([]),
                new RandomProvider(), World.Clock, World.SessionRegistry, World.Create<AggroService>(),
                World.Create<GameObject.Services.MapItemGenerationService.ItemThrowService>(), NullLogger.Instance,
                World.LogLanguageLocalizer, World.GameLanguageLocalizer, TimeSpan.FromSeconds(30));
        }

        private Task ThePlayerEntersRoom_(int room) => Enter(_session, room);

        private Task ThePartnerEntersRoom_(int room) => Enter(_partner, room);

        private async Task Enter(ClientSession session, int room)
        {
            _session.LastPackets.Clear();
            _partner.LastPackets.Clear();
            foreach (var group in _roomSessions.Values)
            {
                group.Invocations.Clear();
            }
            session.Character.MapInstance = _rooms[room];
            await _run.EnteredAsync(session, _rooms[room].MapInstanceId);
        }

        private Task ThePlayerEntersAnOrdinaryMap()
        {
            World.Freeze<Mock<IScriptedInstanceService>>()
                .Setup(s => s.GetRun(It.IsAny<Guid>())).Returns((ScriptedInstanceRun?)null);
            return World.Create<RoomMapExitHandler>()
                .Handle(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance, World.GetBaseMap(1)));
        }

        // "x.y.status" per room, in the order the rooms should arrive.
        private static void RoomsShouldBe(string expected, IEnumerable<IPacket?> packets)
        {
            var wanted = expected.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(room => room.Split('.').Select(int.Parse).ToArray())
                .Select(v => new RsfnPacket { MapIndexX = v[0], MapIndexY = v[1], Status = v[2] })
                .ToList();
            var sent = packets.OfType<RsfnPacket>().ToList();
            Assert.AreEqual(wanted.Count, sent.Count);
            foreach (var (room, actual) in wanted.Zip(sent))
            {
                PacketAssert.AreEqual(room, actual);
            }
        }

        private void ThePlayerShouldBeShownRooms_(string rooms) => RoomsShouldBe(rooms, _session.LastPackets);

        private void ThePartnerShouldBeShownRooms_(string rooms) => RoomsShouldBe(rooms, _partner.LastPackets);

        private void TheRunShouldBeShownRooms_(string rooms) =>
            RoomsShouldBe(rooms, _roomSessions[1].Invocations.SelectMany(call => call.Arguments.OfType<IPacket[]>()).SelectMany(p => p));

        private void ThePlayerShouldBeMarkedAt_(int x, int y) =>
            PacketAssert.AreEqual(new RsfpPacket { MapX = x, MapY = y }, _session.LastPackets.OfType<RsfpPacket>().Single());

        private void TheMapShouldOpen_Times(int times) =>
            Assert.AreEqual(times, _session.LastPackets.OfType<RsfmPacket>().Count(p => p.Type == RsfmPacketType.Open));

        private void TheMapShouldOpenForThePartner() =>
            Assert.AreEqual(1, _partner.LastPackets.OfType<RsfmPacket>().Count(p => p.Type == RsfmPacketType.Open));
    }
}
