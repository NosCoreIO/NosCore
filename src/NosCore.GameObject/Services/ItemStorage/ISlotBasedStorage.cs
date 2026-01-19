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

using NosCore.GameObject.Services.ItemGenerationService.Item;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemStorage
{
    public interface ISlotBasedStorage<TItem, TSlotType>
        where TItem : class, ISlotItem
        where TSlotType : struct, Enum
    {
        int GetMaxSlots(TSlotType slotType);

        TItem? GetBySlot(short slot, TSlotType slotType);

        TItem? GetById(Guid id);

        IEnumerable<TItem> GetAllBySlotType(TSlotType slotType);

        IEnumerable<TItem> GetAll();

        short? GetFreeSlot(TSlotType slotType);

        bool HasFreeSlot(TSlotType slotType);

        int CountBySlotType(TSlotType slotType);

        int CountItem(int itemVNum);
    }

    public interface IMutableSlotBasedStorage<TItem, TSlotType> : ISlotBasedStorage<TItem, TSlotType>
        where TItem : class, ISlotItem
        where TSlotType : struct, Enum
    {
        List<TItem>? AddItem(TItem item, TSlotType? slotType = null, short? slot = null);

        TItem? RemoveBySlot(short slot, TSlotType slotType);

        TItem? RemoveById(Guid id);

        TItem? RemoveItemAmount(short amount, Guid id);

        TItem? MoveItem(short sourceSlot, TSlotType sourceType, TSlotType targetType, short? targetSlot = null, bool swap = false);

        bool TryMoveItem(TSlotType slotType, short sourceSlot, short amount, short destinationSlot,
            out TItem? sourcePocket, out TItem? destinationPocket);

        bool EnoughPlace(List<IItemInstance> items, TSlotType slotType);
    }
}
