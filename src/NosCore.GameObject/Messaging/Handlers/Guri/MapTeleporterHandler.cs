//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MapTeleporterService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Comes back from a map teleporter's bar with the destination cell and the NPC's id.
    [UsedImplicitly]
    public sealed class MapTeleporterHandler(IMapTeleporterService mapTeleporterService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            return evt.Packet is { Type: GuriPacketType.MapTeleporter, Argument: { } x, VisualId: { } y, Data: { } mapNpcId }
                ? mapTeleporterService.CompleteAsync(evt.ClientSession.Character, (int)mapNpcId, (short)x, (short)y)
                : Task.CompletedTask;
        }
    }
}
