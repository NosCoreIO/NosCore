//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.WarehouseService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub
{
    public class WarehouseHub(IWarehouseService warehouseService) : Hub, IWarehouseHub
    {
        public Task<List<WarehouseLink>> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot) => Task.FromResult(warehouseService.GetItems(id, ownerId, warehouseType, slot));

        public Task<bool> DeleteWarehouseItemAsync(Guid id) => warehouseService.WithdrawItemAsync(id);

        public Task<bool> AddWarehouseItemAsync(WareHouseDepositRequest depositRequest) => warehouseService.DepositItemAsync(depositRequest.OwnerId, depositRequest.WarehouseType, depositRequest.ItemInstance, depositRequest.Slot);
    }
}
