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
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class WarehouseController : Controller
    {
        private readonly IDao<IItemInstanceDto?, Guid> _itemInstanceDao;
        private readonly IDao<WarehouseDto, Guid> _warehouseDao;
        private readonly IDao<WarehouseItemDto, Guid> _warehouseItemDao;

        public WarehouseController(IDao<WarehouseItemDto, Guid> warehouseItemDao,
            IDao<WarehouseDto, Guid> warehouseDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        {
            _itemInstanceDao = itemInstanceDao;
            _warehouseItemDao = warehouseItemDao;
            _warehouseDao = warehouseDao;
        }

        [HttpGet]
        public List<WarehouseLink> GetWarehouseItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot)
        {
            var list = new List<WarehouseLink>();
            if (id == null)
            {
                var warehouse = _warehouseDao.FirstOrDefaultAsync(s
                    => s.Type == warehouseType
                    && s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId)
                    && s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null));
                if (slot == null)
                {
                    //todo add
                }
                else
                {
                    //todo add
                }
            }
            else
            {
                var warehouseLink = new WarehouseLink();
                list.Add(warehouseLink);
            }

            return list;
        }


        [HttpDelete]
        public async Task<bool> DeleteWarehouseItemAsync(Guid id)
        {
            var item = await _warehouseItemDao.FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
            if (item == null)
            {
                return false;
            }
            await _warehouseItemDao.TryDeleteAsync(item.Id).ConfigureAwait(false);
            await _warehouseDao.TryDeleteAsync(item.WarehouseId).ConfigureAwait(false);
            await _itemInstanceDao.TryDeleteAsync(item.ItemInstanceId).ConfigureAwait(false);
            return true;
        }

        [HttpPost]
        public async Task<bool> AddWarehouseItemAsync([FromBody] WareHouseDepositRequest depositRequest)
        {
            var item = depositRequest.ItemInstance as IItemInstanceDto;
            item!.Id = Guid.NewGuid();
            item = await _itemInstanceDao.TryInsertOrUpdateAsync(item).ConfigureAwait(true);
            var warehouse = new WarehouseDto
            {
                CharacterId = depositRequest.WarehouseType == WarehouseType.FamilyWareHouse ? null
                    : (long?) depositRequest.OwnerId,
                Id = Guid.NewGuid(),
                FamilyId = depositRequest.WarehouseType == WarehouseType.FamilyWareHouse
                    ? (long?) depositRequest.OwnerId : null,
                Type = depositRequest.WarehouseType,
            };
            warehouse = await _warehouseDao.TryInsertOrUpdateAsync( warehouse).ConfigureAwait(true);
            var warehouseItem = new WarehouseItemDto
            {
                Slot = depositRequest.Slot,
                Id = Guid.NewGuid(),
                ItemInstanceId = item!.Id,
                WarehouseId = warehouse.Id
            };
            await _warehouseItemDao.TryInsertOrUpdateAsync(warehouseItem).ConfigureAwait(true);
            return true;
        }
    }
}