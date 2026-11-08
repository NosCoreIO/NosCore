//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;

public interface IWarehouseHub
{
    Task<List<WarehouseLink>> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot);
    Task<bool> DeleteWarehouseItemAsync(Guid id);
    Task<bool> AddWarehouseItemAsync(WareHouseDepositRequest depositRequest);
    Task<bool> MoveWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot);
    Task<bool> TakeWarehouseItemAsync(Guid id, short amount);
    Task<bool> ReturnWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short slot, ItemInstanceDto itemInstance);
    Task<bool> SplitWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot, ItemInstanceDto split);
}
