//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by UseItemPacketHandler when a player uses an inventory item. Wolverine fans out
    // to every handler with a Handle/HandleAsync(ItemUsedEvent) method; each handler filters by
    // ItemType/Effect internally.
    public sealed record ItemUsedEvent(ClientSession ClientSession, InventoryItemInstance InventoryItem, UseItemPacket Packet);
}
