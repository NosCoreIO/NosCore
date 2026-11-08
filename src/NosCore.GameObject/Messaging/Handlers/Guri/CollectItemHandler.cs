//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.CollectService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Comes back from the collect bar with the map object's id as argument.
    [UsedImplicitly]
    public sealed class CollectItemHandler(ICollectService collectService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            return evt.Packet.Type == GuriPacketType.CollectItem && evt.Packet.Argument is { } mapNpcId
                ? collectService.CompleteAsync(evt.ClientSession.Character, mapNpcId)
                : Task.CompletedTask;
        }
    }
}
