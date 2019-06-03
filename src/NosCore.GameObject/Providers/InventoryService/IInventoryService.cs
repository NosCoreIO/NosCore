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
using ChickenAPI.Packets.Enumerations;
using NosCore.Data;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.InventoryService
{
    public interface IInventoryService : IDictionary<Guid, InventoryItemInstance>
    {
        bool IsExpanded { get; set; }

        List<InventoryItemInstance> AddItemToPocket(InventoryItemInstance newItem);
        List<InventoryItemInstance> AddItemToPocket(InventoryItemInstance newItem, PocketType? type);
        List<InventoryItemInstance> AddItemToPocket(InventoryItemInstance newItem, PocketType? type, short? slot);
        bool CanAddItem(short itemVnum);
        int CountItem(int itemVNum);
        int CountItemInAnPocket(PocketType inv);
        InventoryItemInstance DeleteById(Guid id);
        InventoryItemInstance DeleteFromTypeAndSlot(PocketType type, short slot);
        InventoryItemInstance LoadByItemInstanceId(Guid id);
        InventoryItemInstance LoadBySlotAndType(short slot, PocketType type);

        InventoryItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType);

        InventoryItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType,
            short? targetSlot, bool swap);

        bool TryMoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out InventoryItemInstance sourcePocket, out InventoryItemInstance destinationPocket);

        bool EnoughPlace(List<IItemInstance> itemInstances, PocketType type);
        InventoryItemInstance RemoveItemAmountFromInventory(short amount, Guid id);
    }
}