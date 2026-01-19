//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public interface IWarehouseService
    {
        int GetMaxSlots(WarehouseType warehouseType);

        List<WarehouseLink> GetItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot);

        List<WarehouseItem> GetWarehouseItems(long ownerId, WarehouseType warehouseType);

        WarehouseItem? GetItemBySlot(long ownerId, WarehouseType warehouseType, short slot);

        short? GetFreeSlot(long ownerId, WarehouseType warehouseType);

        Task<bool> WithdrawItemAsync(Guid id);

        Task<WarehouseItem?> WithdrawItemFromSlotAsync(long ownerId, WarehouseType warehouseType, short slot);

        Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, ItemInstanceDto? itemInstance, short slot);

        Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, IItemInstance itemInstance);

        Task<bool> MoveItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot);
    }
}
