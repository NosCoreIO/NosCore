//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenShopHandler
    {
        [UsedImplicitly]
        public Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.OpenShop || evt.Target == null)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.HandlePacketsAsync(new[]
            {
                new ShoppingPacket
                {
                    VisualType = evt.Packet.VisualType ?? 0,
                    VisualId = evt.Packet.VisualId ?? 0,
                    ShopType = evt.Packet.Type ?? 0,
                    Unknown = 0
                }
            });
        }
    }
}
