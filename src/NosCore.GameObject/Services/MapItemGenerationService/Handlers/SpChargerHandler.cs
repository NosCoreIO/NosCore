//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Drops;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class SpChargerEventHandler : IGetMapItemEventHandler
    {
        public bool Condition(MapItem item)
        {
            return (item.ItemInstance!.Item.ItemType == ItemType.Map) &&
                (item.ItemInstance.Item.Effect == ItemEffectType.SpCharger);
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            await requestData.ClientSession.Character.AddSpPointsAsync(requestData.Data.Item1.ItemInstance!.Item.EffectValue);
            await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateSpPoint());
            requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}
