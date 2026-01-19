//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub
{
    public class WarehouseHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(WarehouseHub), logger), IWarehouseHub
    {
        public Task<List<WarehouseLink>> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot) =>
            InvokeAsync<List<WarehouseLink>>(nameof(GetWarehouseItems), id, ownerId, warehouseType, slot);

        public Task<bool> DeleteWarehouseItemAsync(Guid id) =>
            InvokeAsync<bool>(nameof(DeleteWarehouseItemAsync), id);

        public Task<bool> AddWarehouseItemAsync(WareHouseDepositRequest depositRequest) =>
            InvokeAsync<bool>(nameof(AddWarehouseItemAsync), depositRequest);
    }
}
