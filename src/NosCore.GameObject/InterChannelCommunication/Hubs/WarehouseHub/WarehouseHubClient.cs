//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub
{
    public class WarehouseHubClient(IMessageBus bus) : IWarehouseHub
    {
        public Task<List<WarehouseLink>> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot) =>
            bus.RequestAsync<List<WarehouseLink>>(new GetWarehouseItemsRequest(id, ownerId, warehouseType, slot));

        public Task<bool> DeleteWarehouseItemAsync(Guid id) => bus.RequestAsync<bool>(new DeleteWarehouseItemRequest(id));

        public Task<bool> AddWarehouseItemAsync(WareHouseDepositRequest depositRequest) =>
            bus.RequestAsync<bool>(new AddWarehouseItemRequest(depositRequest));
    }
}
