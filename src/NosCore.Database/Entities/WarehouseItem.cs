//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class WarehouseItem : SynchronizableBaseEntity
    {
        [Required]
        public virtual Warehouse Warehouse { get; set; } = null!;

        [Required]
        public Guid WarehouseId { get; set; }

        [Required]
        public virtual ItemInstance ItemInstance { get; set; } = null!;

        [Required]
        public Guid ItemInstanceId { get; set; }

        [Required]
        public short Slot { get; set; }
    }
}
