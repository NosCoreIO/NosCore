//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ServerPackets.MiniMap;

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    // Outside a run the client keeps no room marker, so a time-space left behind does not stay drawn.
    [UsedImplicitly]
    public sealed class RoomMapExitHandler(IScriptedInstanceService scriptedInstanceService)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) =>
            scriptedInstanceService.GetRun(evt.MapInstance.MapInstanceId) == null
                ? evt.ClientSession.SendPacketAsync(new RsfpPacket { MapX = 0, MapY = -1 })
                : Task.CompletedTask;
    }
}
