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

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.WarehouseService
{
    public class WarehouseService(IDao<WarehouseItemDto, Guid> warehouseItemDao,
            IDao<WarehouseDto, Guid> warehouseDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao)
        : IWarehouseService
    {
        public List<WarehouseLink> GetItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot)
        {
            var list = new List<WarehouseLink>();
            if (id == null)
            {
                var warehouse = warehouseDao.FirstOrDefaultAsync(s
                    => s.Type == warehouseType
                    && s.CharacterId == (warehouseType == WarehouseType.FamilyWareHouse ? null : ownerId)
                    && s.FamilyId == (warehouseType == WarehouseType.FamilyWareHouse ? ownerId : null));
                if (slot == null)
                {
                    //todo add
                }
                //todo add
            }
            else
            {
                var warehouseLink = new WarehouseLink();
                list.Add(warehouseLink);
            }

            return list;
        }

        public async Task<bool> WithdrawItemAsync(Guid id)
        {
            var item = await warehouseItemDao.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null)
            {
                return false;
            }
            await warehouseItemDao.TryDeleteAsync(item.Id);
            await warehouseDao.TryDeleteAsync(item.WarehouseId);
            await itemInstanceDao.TryDeleteAsync(item.ItemInstanceId);
            return true;
        }

        public async Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, ItemInstanceDto? itemInstance, short slot)
        {
            var item = itemInstance as IItemInstanceDto;
            item!.Id = Guid.NewGuid();
            item = await itemInstanceDao.TryInsertOrUpdateAsync(item).ConfigureAwait(true);
            var warehouse = new WarehouseDto
            {
                CharacterId = warehouseType == WarehouseType.FamilyWareHouse ? null
                    : (long?)ownerId,
                Id = Guid.NewGuid(),
                FamilyId = warehouseType == WarehouseType.FamilyWareHouse
                    ? (long?)ownerId : null,
                Type = warehouseType,
            };
            warehouse = await warehouseDao.TryInsertOrUpdateAsync(warehouse).ConfigureAwait(true);
            var warehouseItem = new WarehouseItemDto
            {
                Slot = slot,
                Id = Guid.NewGuid(),
                ItemInstanceId = item!.Id,
                WarehouseId = warehouse.Id
            };
            await warehouseItemDao.TryInsertOrUpdateAsync(warehouseItem).ConfigureAwait(true);
            return true;
        }
    }
}