//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class FairyLookHandler
    {
        [UsedImplicitly]
        public Task Handle(FairyLookChangedEvent evt)
        {
            if (evt.Target is not PlayerComponentBundle player || player.MapInstance == null
                || player.InventoryService.LoadBySlotAndType((short)EquipmentType.Fairy, NoscorePocketType.Wear)
                    ?.ItemInstance is not WearableInstance fairy)
            {
                return Task.CompletedTask;
            }

            return player.MapInstance.SendPacketAsync(player.GeneratePairy(fairy));
        }
    }
}
