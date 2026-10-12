//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.WarehouseService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub
{
    public class WarehouseHub(IWarehouseService warehouseService) : IWarehouseHub
    {
        public Task<List<WarehouseLink>> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot) => warehouseService.GetItemsAsync(id, ownerId, warehouseType, slot);

        public Task<bool> DeleteWarehouseItemAsync(Guid id) => warehouseService.WithdrawItemAsync(id);

        public Task<bool> AddWarehouseItemAsync(WareHouseDepositRequest depositRequest) => warehouseService.DepositItemAsync(depositRequest.OwnerId, depositRequest.WarehouseType, depositRequest.ItemInstance, depositRequest.Slot);

        public static async Task<Reply<List<WarehouseLink>>> Handle(GetWarehouseItemsRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().GetWarehouseItems(r.Id, r.OwnerId, r.WarehouseType, r.Slot));

        public static async Task<Reply<bool>> Handle(DeleteWarehouseItemRequest r, IServiceProvider services) => new(await services.GetRequiredService<WarehouseHub>().DeleteWarehouseItemAsync(r.Id));

        public static async Task<Reply<bool>> Handle(AddWarehouseItemRequest r, IServiceProvider services) => new(await services.GetRequiredService<WarehouseHub>().AddWarehouseItemAsync(r.DepositRequest));
    }
}
