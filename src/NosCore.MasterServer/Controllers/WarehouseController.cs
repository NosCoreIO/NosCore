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
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class WarehouseController : Controller
    {
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao;
        private readonly IGenericDao<WarehouseDto> _warehouseDao;
        private readonly IGenericDao<WarehouseItemDto> _warehouseItemDao;

        public WarehouseController(IGenericDao<WarehouseItemDto> warehouseItemDao,
            IGenericDao<WarehouseDto> warehouseDao, IGenericDao<IItemInstanceDto> itemInstanceDao)
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
                var warehouse = _warehouseDao.FirstOrDefault(s
                    => s.Type == warehouseType
                    && s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId)
                    && s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null));
            }
            else
            {
                var warehouseLink = new WarehouseLink { };
                list.Add(warehouseLink);
            }

            return list;
        }


        [HttpDelete]
        public bool DeleteWarehouseItem(Guid id)
        {
            var item = _warehouseItemDao.FirstOrDefault(s => s.Id == id);
            _warehouseItemDao.Delete(item.Id);
            _warehouseDao.Delete(item.WarehouseId);
            _itemInstanceDao.Delete(item.ItemInstanceId);
            return true;
        }

        [HttpPost]
        public bool AddWarehouseItem([FromBody] WareHouseDepositRequest depositRequest)
        {
            var item = depositRequest.ItemInstance as IItemInstanceDto;
            item.Id = Guid.NewGuid();
            _itemInstanceDao.InsertOrUpdate(ref item);
            var warehouse = new WarehouseDto
            {
                CharacterId = depositRequest.WarehouseType == WarehouseType.FamilyWareHouse ? null
                    : (long?) depositRequest.OwnerId,
                Id = Guid.NewGuid(),
                FamilyId = depositRequest.WarehouseType == WarehouseType.FamilyWareHouse
                    ? (long?) depositRequest.OwnerId : null,
                Type = depositRequest.WarehouseType,
            };
            _warehouseDao.InsertOrUpdate(ref warehouse);
            var warehouseItem = new WarehouseItemDto
            {
                Slot = depositRequest.Slot,
                Id = Guid.NewGuid(),
                ItemInstanceId = item.Id,
                WarehouseId = warehouse.Id
            };
            _warehouseItemDao.InsertOrUpdate(ref warehouseItem);
            return true;
        }
    }
}