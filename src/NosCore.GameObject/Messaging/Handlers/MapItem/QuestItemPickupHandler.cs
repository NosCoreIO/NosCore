//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class QuestItemPickupHandler(IGameEventDispatcher gameEvents)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            var item = evt.ItemInstance;
            if (item?.Item.ItemType != ItemType.Map || item.Item.Effect != ItemEffectType.RaidCollectQuestItem)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            var mapInstance = character.MapInstance;
            if (!mapInstance.TryRemoveMapItem(evt.VisualId))
            {
                return;
            }

            await mapInstance.SendPacketAsync(evt.GenerateGet());
            await gameEvents.RaiseAsync(new ItemsReceivedEvent(character, evt.VNum, evt.Amount));
        }
    }
}
