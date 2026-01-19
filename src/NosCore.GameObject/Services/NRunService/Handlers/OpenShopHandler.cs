//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class OpenShopEventHandler : INrunEventHandler
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.OpenShop) && (item.Item1 != null);
        }

        public Task ExecuteAsync(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            return requestData.ClientSession.HandlePacketsAsync(new[]
            {
                new ShoppingPacket
                {
                    VisualType = requestData.Data.Item2.VisualType ?? 0,
                    VisualId = requestData.Data.Item2.VisualId ?? 0,
                    ShopType = requestData.Data.Item2.Type ?? 0,
                    Unknown = 0
                }
            });
        }
    }
}
