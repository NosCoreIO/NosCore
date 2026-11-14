//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.RaidService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class RaidSealHandler(IRaidService raidService)
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            return evt.InventoryItem.ItemInstance.Item.Effect == ItemEffectType.RaidSeal
                ? raidService.OpenAsync(evt.ClientSession, evt.InventoryItem)
                : Task.CompletedTask;
        }
    }
}
