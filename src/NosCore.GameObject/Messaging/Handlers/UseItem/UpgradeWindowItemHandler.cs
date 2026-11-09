//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // The item is only spent by the upgrade the window sends back.
    [UsedImplicitly]
    public sealed class UpgradeWindowItemHandler(IEnumerable<IUpgradeOperation> operations,
        INpcDialogService npcDialogService)
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            var vnum = evt.InventoryItem.ItemInstance.ItemVNum;
            var operation = operations.OfType<IItemOpenedUpgradeOperation>()
                .FirstOrDefault(o => o.OpeningItemVNums.Contains(vnum));
            if (operation is null || evt.ClientSession.Character.InExchangeOrShop)
            {
                return Task.CompletedTask;
            }

            npcDialogService.OpenRemotely(evt.ClientSession.Character, operation.Window);
            return evt.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)operation.Kind,
            });
        }
    }
}
