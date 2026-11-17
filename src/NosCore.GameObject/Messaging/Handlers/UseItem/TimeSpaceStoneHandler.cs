//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.ScriptedInstanceService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class TimeSpaceStoneHandler(ITimeSpaceEntryService timeSpaceEntryService)
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            return evt.InventoryItem.ItemInstance.Item.Effect == ItemEffectType.TimeSpaceStone
                ? timeSpaceEntryService.EnterWithStoneAsync(evt.ClientSession, evt.InventoryItem)
                : Task.CompletedTask;
        }
    }
}
