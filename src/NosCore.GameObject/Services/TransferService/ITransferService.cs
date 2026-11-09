//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.TransferService
{
    // Gold and bank gold are in gold. Items map an inventory entry to the amount given from it.
    public sealed record TransferOffer(long Gold, long BankGold, IReadOnlyDictionary<InventoryItemInstance, short> Items);

    public interface ITransferService
    {
        // Moves both offers between the two characters and persists both sides in one database
        // transaction. Returns the inventory changes to send, keyed by character id, or null when
        // nothing changed: an offer no longer covered, no room, or a failed write.
        Task<List<KeyValuePair<long, IvnPacket>>?> TransferAsync(PlayerComponentBundle first, TransferOffer firstOffer,
            PlayerComponentBundle second, TransferOffer secondOffer);
    }
}
