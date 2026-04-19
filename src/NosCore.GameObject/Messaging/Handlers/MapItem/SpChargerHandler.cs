//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class SpChargerHandler(IOptions<WorldConfiguration> worldConfiguration)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            var mapItem = evt.MapItem;
            if (mapItem.ItemInstance!.Item.ItemType != ItemType.Map
                || mapItem.ItemInstance.Item.Effect != ItemEffectType.SpCharger)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            character.AddSpPoints(mapItem.ItemInstance.Item.EffectValue, worldConfiguration);
            var mapInstance = character.MapInstance;
            await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
            mapInstance.TryRemoveMapItem(mapItem.VisualId);
            await mapInstance.SendPacketAsync(character.GenerateGet(mapItem.VisualId));
        }
    }
}
