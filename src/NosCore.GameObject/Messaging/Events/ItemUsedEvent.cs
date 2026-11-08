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
    // Invoked inline by UseItemPacketHandler, so every handler runs under the session's packet lock.
    // Each handler filters by ItemType/Effect internally.
    public sealed record ItemUsedEvent(ClientSession ClientSession, InventoryItemInstance InventoryItem, UseItemPacket Packet);
}
