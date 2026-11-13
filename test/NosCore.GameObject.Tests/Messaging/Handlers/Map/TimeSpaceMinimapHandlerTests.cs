//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.TimeSpaces;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Map
{
    [TestClass]
    public class TimeSpaceMinimapHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IScriptedInstanceService> _scriptedInstanceService;
        private TimeSpaceMinimapHandler _handler;
        private ScriptedInstanceRun _run;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _scriptedInstanceService = World.Freeze<Mock<IScriptedInstanceService>>();
            _handler = World.Create<TimeSpaceMinimapHandler>();
        }

        [TestMethod]
        public async Task EnteringTheFirstRoomDrawsTheWholeMap()
        {
            await new Spec("Entering a time-space draws every room, marks the player's room and opens the map")
                .Given(TheCurrentMapIsRoom_OfAThreeRoomTimeSpace, 0)
                .WhenAsync(EnteringTheMap)
                .Then(RoomsShouldBeSentAs_, "3.11.0 3.10.1 3.9.1")
                .And(ThePlayerShouldBeShownAt_, "3.11")
                .And(TheMapShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EnteringANewRoomMarksOnlyThatRoom()
        {
            await new Spec("Walking into a room not yet visited marks it and moves the player's marker")
                .Given(TheCurrentMapIsRoom_OfAThreeRoomTimeSpace, 1)
                .And(Room_WasVisited, 0)
                .WhenAsync(EnteringTheMap)
                .Then(RoomsShouldBeSentAs_, "3.10.0")
                .And(ThePlayerShouldBeShownAt_, "3.10")
                .And(TheMapShouldNotReopen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComingBackToAVisitedRoomOnlyMovesTheMarker()
        {
            await new Spec("Walking back into a visited room only moves the player's marker")
                .Given(TheCurrentMapIsRoom_OfAThreeRoomTimeSpace, 0)
                .And(Room_WasVisited, 0)
                .And(Room_WasVisited, 1)
                .WhenAsync(EnteringTheMap)
                .Then(RoomsShouldBeSentAs_, "")
                .And(ThePlayerShouldBeShownAt_, "3.11")
                .And(TheMapShouldNotReopen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OutsideATimeSpaceTheRoomMapIsCleared()
        {
            await new Spec("A map outside any time-space clears the room marker")
                .Given(TheCurrentMapIsNotATimeSpace)
                .WhenAsync(EnteringTheMap)
                .Then(ThePlayerShouldBeShownAt_, "0.-1")
                .And(RoomsShouldBeSentAs_, "")
                .And(TheMapShouldNotReopen)
                .ExecuteAsync();
        }

        private void TheCurrentMapIsRoom_OfAThreeRoomTimeSpace(int key)
        {
            var rooms = new List<InstanceRoom>
            {
                new(0, 1, 3, 11),
                new(1, 1, 3, 10),
                new(2, 1, 3, 9),
            };
            var entrance = new ScriptedInstance { Definition = new ScriptedInstanceDefinition { Rooms = rooms } };
            var ids = rooms.ToDictionary(r => r.Key, r => r.Key == key ? _session.Character.MapInstanceId : Guid.NewGuid());
            _run = new ScriptedInstanceRun(entrance, ids, Instant.FromUnixTimeSeconds(0));
            var mapInstanceId = _session.Character.MapInstanceId;
            _scriptedInstanceService.Setup(s => s.GetRun(mapInstanceId)).Returns(_run);
        }

        private void Room_WasVisited(int key) => _run.VisitedRooms.TryAdd(key, 0);

        private void TheCurrentMapIsNotATimeSpace() =>
            _scriptedInstanceService.Setup(s => s.GetRun(It.IsAny<Guid>())).Returns((ScriptedInstanceRun?)null);

        private Task EnteringTheMap() =>
            _handler.Handle(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance, World.GetBaseMap(1)));

        private void RoomsShouldBeSentAs_(string rooms) =>
            Assert.AreEqual(rooms, string.Join(' ', _session.LastPackets.OfType<RsfnPacket>()
                .Select(p => $"{p.MapIndexX}.{p.MapIndexY}.{p.Status}")));

        private void ThePlayerShouldBeShownAt_(string position)
        {
            var packet = _session.LastPackets.OfType<RsfpPacket>().Single();
            Assert.AreEqual(position, $"{packet.MapX}.{packet.MapY}");
        }

        private void TheMapShouldOpen() =>
            Assert.AreEqual(RsfmPacketType.Open, _session.LastPackets.OfType<RsfmPacket>().Single().Type);

        private void TheMapShouldNotReopen() =>
            Assert.IsFalse(_session.LastPackets.OfType<RsfmPacket>().Any());
    }
}
