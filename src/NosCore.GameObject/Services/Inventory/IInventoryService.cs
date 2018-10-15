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
    public interface IInventoryService : IDictionary<Guid, ItemInstance>
    {
        bool IsExpanded { get; set; }

        List<ItemInstance> AddItemToPocket(ItemInstance newItem, PocketType? type = null, short? slot = null);
        bool CanAddItem(short itemVnum);
        int CountItem(int itemVNum);
        int CountItemInAnPocket(PocketType inv);
        ItemInstance DeleteById(Guid id);
        ItemInstance DeleteFromTypeAndSlot(PocketType type, short slot);
        T LoadByItemInstanceId<T>(Guid id) where T : ItemInstance;
        T LoadBySlotAndType<T>(short slot, PocketType type) where T : ItemInstance;

        ItemInstance MoveInPocket(short sourceSlot, PocketType sourceType, PocketType targetType,
            short? targetSlot = null, bool wear = true);

        void MoveItem(PocketType sourcetype, short sourceSlot, short amount, short destinationSlot,
            out ItemInstance sourcePocket, out ItemInstance destinationPocket);
    }
}