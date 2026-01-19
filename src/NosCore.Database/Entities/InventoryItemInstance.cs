//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Database.Entities.Base;
using System;
using System.Collections.Generic;

namespace NosCore.Database.Entities
{
    public class InventoryItemInstance : SynchronizableBaseEntity
    {
        public InventoryItemInstance()
        {
            MinilandObject = new HashSet<MinilandObject>();
        }


        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public virtual ItemInstance ItemInstance { get; set; } = null!;

        public Guid ItemInstanceId { get; set; }

        public virtual ICollection<MinilandObject> MinilandObject { get; set; }

        public short Slot { get; set; }

        public NoscorePocketType Type { get; set; }
    }
}
