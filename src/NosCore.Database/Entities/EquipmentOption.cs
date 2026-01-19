//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class EquipmentOption : SynchronizableBaseEntity
    {
        public byte Level { get; set; }

        public byte Type { get; set; }

        public int Value { get; set; }

        [ForeignKey(nameof(WearableInstanceId))]
        public virtual WearableInstance WearableInstance { get; set; } = null!;

        public Guid WearableInstanceId { get; set; }
    }
}
