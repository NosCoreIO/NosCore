//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Answers the teleport question a tutorial run step opens: the yes repeats the offered map and cell.
    [UsedImplicitly]
    public sealed class ScriptTeleportHandler(IMapChangeService mapChangeService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var player = evt.ClientSession.Character;
            switch (evt.Packet.Type)
            {
                case GuriPacketType.ScriptTeleportDeclined:
                    player.OfferedScriptTeleport = null;
                    return Task.CompletedTask;
                case GuriPacketType.ScriptTeleport
                    when player.OfferedScriptTeleport is { TeleportMapId: { } mapId, TeleportX: { } x, TeleportY: { } y }
                    && evt.Packet is { Argument: { } argument, VisualId: { } visualId, Data: { } data }
                    && argument == mapId && visualId == x && data == y:
                    player.OfferedScriptTeleport = null;
                    return mapChangeService.ChangeMapAsync(evt.ClientSession, mapId, x, y);
                default:
                    return Task.CompletedTask;
            }
        }
    }
}
