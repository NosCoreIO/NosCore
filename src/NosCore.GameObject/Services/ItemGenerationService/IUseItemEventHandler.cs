//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using System;

namespace NosCore.GameObject.Services.ItemGenerationService
{
    public interface IUseItemEventHandler : IEventHandler<Item.Item, Tuple<InventoryItemInstance, UseItemPacket>>
    {
    }
}
