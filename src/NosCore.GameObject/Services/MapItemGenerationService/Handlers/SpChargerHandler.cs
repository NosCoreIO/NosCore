//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Drops;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class SpChargerEventHandler(IOptions<WorldConfiguration> worldConfiguration) : IGetMapItemEventHandler
    {
        public bool Condition(MapItem item)
        {
            return (item.ItemInstance!.Item.ItemType == ItemType.Map) &&
                (item.ItemInstance.Item.Effect == ItemEffectType.SpCharger);
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            var session = requestData.ClientSession;
            var mapItem = requestData.Data.Item1;

            var character = session.Character;
            character.AddSpPoints(mapItem.ItemInstance!.Item.EffectValue, worldConfiguration);
            var spPointPacket = character.GenerateSpPoint(worldConfiguration);
            var mapInstance = character.MapInstance;
            var getPacket = character.GenerateGet(mapItem.VisualId);

            await session.SendPacketAsync(spPointPacket);
            mapInstance.MapItems.TryRemove(mapItem.VisualId, out _);
            await mapInstance.SendPacketAsync(getPacket);
        }
    }
}
