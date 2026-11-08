//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.Dto;
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

        public Task<bool> MoveWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot) =>
            warehouseService.MoveItemAsync(ownerId, warehouseType, sourceSlot, destinationSlot);

        public Task<bool> TakeWarehouseItemAsync(Guid id, short amount) => warehouseService.TakeItemAsync(id, amount);

        public Task<bool> ReturnWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short slot, ItemInstanceDto itemInstance) =>
            warehouseService.ReturnItemAsync(ownerId, warehouseType, slot, itemInstance);

        public Task<bool> SplitWarehouseItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot, ItemInstanceDto split) =>
            warehouseService.SplitItemAsync(ownerId, warehouseType, sourceSlot, destinationSlot, split);

        public static async Task<Reply<List<WarehouseLink>>> Handle(GetWarehouseItemsRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().GetWarehouseItems(r.Id, r.OwnerId, r.WarehouseType, r.Slot));

        public static async Task<Reply<bool>> Handle(DeleteWarehouseItemRequest r, IServiceProvider services) => new(await services.GetRequiredService<WarehouseHub>().DeleteWarehouseItemAsync(r.Id));

        public static async Task<Reply<bool>> Handle(MoveWarehouseItemRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().MoveWarehouseItemAsync(r.OwnerId, r.WarehouseType, r.SourceSlot, r.DestinationSlot));

        public static async Task<Reply<bool>> Handle(AddWarehouseItemRequest r, IServiceProvider services) => new(await services.GetRequiredService<WarehouseHub>().AddWarehouseItemAsync(r.DepositRequest));

        public static async Task<Reply<bool>> Handle(TakeWarehouseItemRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().TakeWarehouseItemAsync(r.Id, r.Amount));

        public static async Task<Reply<bool>> Handle(ReturnWarehouseItemRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().ReturnWarehouseItemAsync(r.OwnerId, r.WarehouseType, r.Slot, r.ItemInstance));

        public static async Task<Reply<bool>> Handle(SplitWarehouseItemRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<WarehouseHub>().SplitWarehouseItemAsync(r.OwnerId, r.WarehouseType, r.SourceSlot, r.DestinationSlot, r.Split));
    }
}
