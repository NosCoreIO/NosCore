//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public readonly record struct WarehouseSlots(long OwnerId, WarehouseType Type, int Size);

    public readonly record struct StoredItem(Guid LinkId, short Slot, IItemInstance Item);

    public interface IWarehouseTransferService
    {
        Task<List<StoredItem>> GetItemsAsync(long ownerId, WarehouseType warehouseType);

        Task<IItemInstance?> DepositAsync(ClientSession session, WarehouseSlots warehouse, PocketType pocket, short slot,
            short amount, short warehouseSlot, Func<IItemInstance, bool> canStore);

        Task<IItemInstance?> WithdrawAsync(ClientSession session, WarehouseSlots warehouse, short warehouseSlot, short amount);

        Task MoveAsync(ClientSession session, WarehouseSlots warehouse, short sourceSlot, short amount, short destinationSlot);

        Task SendSlotAsync(ClientSession session, WarehouseType warehouseType, IItemInstance? item, short slot);
    }
}
