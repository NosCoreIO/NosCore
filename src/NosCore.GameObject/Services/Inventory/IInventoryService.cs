//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services.Inventory
{
    public interface IInventoryService : IDictionary<Guid, IItemInstance>
    {
        bool IsExpanded { get; set; }

        List<IItemInstance> AddItemToPocket(IItemInstance newItem);
        List<IItemInstance> AddItemToPocket(IItemInstance newItem, PocketType? type);
        List<IItemInstance> AddItemToPocket(IItemInstance newItem, PocketType? type, short? slot);
        bool CanAddItem(short itemVnum);
        int CountItem(int itemVNum);
        int CountItemInAnPocket(PocketType inv);
        IItemInstance DeleteById(Guid id);
        IItemInstance DeleteFromTypeAndSlot(PocketType type, short slot);
        T LoadByItemInstanceId<T>(Guid id) where T : IItemInstance;
        T LoadBySlotAndType<T>(short slot, PocketType type) where T : IItemInstance;

        IItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType);

        IItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType,
            short? targetSlot, bool swap);

        bool TryMoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out IItemInstance sourcePocket, out IItemInstance destinationPocket);

        bool EnoughPlace(List<IItemInstance> itemInstances);
        IItemInstance RemoveItemAmountFromInventory(short amount, Guid id);
    }
}