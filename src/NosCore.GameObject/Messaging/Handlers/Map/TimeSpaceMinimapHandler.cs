//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.TimeSpaces;

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    [UsedImplicitly]
    public sealed class TimeSpaceMinimapHandler(IScriptedInstanceService scriptedInstanceService)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt)
        {
            var session = evt.ClientSession;
            var run = scriptedInstanceService.GetRun(evt.MapInstance.MapInstanceId);
            var rooms = run?.Entrance.Definition?.Rooms ?? [];
            var room = run == null ? null : rooms.FirstOrDefault(r =>
                run.Rooms.TryGetValue(r.Key, out var id) && id == evt.MapInstance.MapInstanceId);
            if (run == null || room == null)
            {
                return session.SendPacketAsync(new RsfpPacket { MapX = 0, MapY = -1 });
            }

            var firstEntry = run.VisitedRooms.IsEmpty;
            var newRoom = run.VisitedRooms.TryAdd(room.Key, 0);
            var packets = new List<IPacket>();
            if (firstEntry)
            {
                packets.AddRange(rooms.Select(r => GenerateRsfn(run, r)));
            }
            else if (newRoom)
            {
                packets.Add(GenerateRsfn(run, room));
            }

            packets.Add(new RsfpPacket { MapX = room.IndexX, MapY = room.IndexY });
            if (firstEntry)
            {
                packets.Add(new RsfmPacket { Type = RsfmPacketType.Open });
            }

            return session.SendPacketsAsync(packets);
        }

        private static RsfnPacket GenerateRsfn(ScriptedInstanceRun run, InstanceRoom room) => new()
        {
            MapIndexX = room.IndexX,
            MapIndexY = room.IndexY,
            Status = run.VisitedRooms.ContainsKey(room.Key) ? 0 : 1
        };
    }
}
