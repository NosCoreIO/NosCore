//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;

namespace NosCore.Database.Entities
{
    public class Warehouse : SynchronizableBaseEntity
    {
        public Warehouse()
        {
            WarehouseItems = new HashSet<WarehouseItem>();
        }

        public virtual Character? Character { get; set; }

        public long? CharacterId { get; set; }

        public virtual Family? Family { get; set; }

        public long? FamilyId { get; set; }

        public virtual ICollection<WarehouseItem> WarehouseItems { get; set; }

        public WarehouseType Type { get; set; }
    }
}
