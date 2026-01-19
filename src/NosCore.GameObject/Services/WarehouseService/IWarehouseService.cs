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

using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.WarehouseService
{
    public interface IWarehouseService
    {
        int GetMaxSlots(WarehouseType warehouseType);

        List<WarehouseLink> GetItems(Guid? id, long? ownerId, WarehouseType warehouseType, byte? slot);

        List<WarehouseItem> GetWarehouseItems(long ownerId, WarehouseType warehouseType);

        WarehouseItem? GetItemBySlot(long ownerId, WarehouseType warehouseType, short slot);

        short? GetFreeSlot(long ownerId, WarehouseType warehouseType);

        Task<bool> WithdrawItemAsync(Guid id);

        Task<WarehouseItem?> WithdrawItemFromSlotAsync(long ownerId, WarehouseType warehouseType, short slot);

        Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, ItemInstanceDto? itemInstance, short slot);

        Task<bool> DepositItemAsync(long ownerId, WarehouseType warehouseType, IItemInstance itemInstance);

        Task<bool> MoveItemAsync(long ownerId, WarehouseType warehouseType, short sourceSlot, short destinationSlot);
    }
}