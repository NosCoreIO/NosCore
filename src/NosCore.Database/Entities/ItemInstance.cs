//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
        public virtual Character? BoundCharacter { get; set; }

        public long? BoundCharacterId { get; set; }

        public long CharacterId { get; set; }

        public short Design { get; set; }

        public int DurabilityPoint { get; set; }

        public virtual Item Item { get; set; } = null!;

        public Instant? ItemDeleteTime { get; set; }

        public short ItemVNum { get; set; }

        public virtual ICollection<InventoryItemInstance> InventoryItemInstance { get; set; }

        public byte Upgrade { get; set; }

        public short Rare { get; set; }
    }
}
