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

using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class WarehouseController : Controller
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public List<WarehouseLink> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot) => _warehouseService.GetItems(id, ownerId, warehouseType, slot);

        [HttpDelete]
        public Task<bool> DeleteWarehouseItemAsync(Guid id) => _warehouseService.WithdrawItemAsync(id);

        [HttpPost]
        public Task<bool> AddWarehouseItemAsync([FromBody] WareHouseDepositRequest depositRequest) => _warehouseService.DepositItemAsync(depositRequest.OwnerId, depositRequest.WarehouseType, depositRequest.ItemInstance, depositRequest.Slot);
    }
}