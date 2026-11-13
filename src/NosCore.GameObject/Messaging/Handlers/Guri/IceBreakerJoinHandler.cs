//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.IceBreakerService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    [UsedImplicitly]
    public sealed class IceBreakerJoinHandler(IIceBreakerService iceBreakerService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            return evt.Packet.Type == GuriPacketType.IceBreaker
                ? iceBreakerService.JoinAsync(evt.ClientSession)
                : Task.CompletedTask;
        }
    }
}
