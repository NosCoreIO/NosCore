//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using Serilog;

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
