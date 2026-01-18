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

using NosCore.Data.Dto;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;

namespace NosCore.GameObject.Services.InventoryService
{
    public class InventoryItemInstance : InventoryItemInstanceDto
    {
        public InventoryItemInstance(IItemInstance itemInstance)
        {
            ItemInstance = itemInstance;
            ItemInstanceId = itemInstance.Id;
        }
        public IItemInstance ItemInstance { get; set; }

        public static InventoryItemInstance Create(IItemInstance it, long characterId)
        {
            return Create(it, characterId, null);
        }

        public static InventoryItemInstance Create(IItemInstance it, long characterId,
            InventoryItemInstanceDto? inventoryItemInstance)
        {
            return new InventoryItemInstance(it)
            {
                Id = inventoryItemInstance?.Id ?? Guid.NewGuid(),
                CharacterId = characterId,
                Slot = inventoryItemInstance?.Slot ?? 0,
                Type = inventoryItemInstance?.Type ?? it.Item.Type
            };
        }
    }
}