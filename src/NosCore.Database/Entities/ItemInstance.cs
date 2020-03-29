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
using System.ComponentModel.DataAnnotations.Schema;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
    public class ItemInstance : SynchronizableBaseEntity
    {
        public ItemInstance()
        {
            BazaarItem = new HashSet<BazaarItem>();
            InventoryItemInstance = new HashSet<InventoryItemInstance>();
            Mail = new HashSet<Mail>();
            WarehouseItems = new HashSet<WarehouseItem>();
        }

        public short Amount { get; set; }

        public virtual ICollection<BazaarItem> BazaarItem { get; set; }

        public virtual ICollection<Mail> Mail { get; set; }

        public virtual ICollection<WarehouseItem> WarehouseItems { get; }

        [ForeignKey(nameof(BoundCharacterId))]
        public virtual Character BoundCharacter { get; set; } = null!;

        public long? BoundCharacterId { get; set; }

        public long CharacterId { get; set; }

        public short Design { get; set; }

        public int DurabilityPoint { get; set; }

        public virtual Item Item { get; set; } = null!;

        public DateTime? ItemDeleteTime { get; set; }

        public short ItemVNum { get; set; }

        public virtual ICollection<InventoryItemInstance> InventoryItemInstance { get; set; }

        public byte Upgrade { get; set; }

        public short Rare { get; set; }
    }
}