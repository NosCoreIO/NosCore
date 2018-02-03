using System;
using System.ComponentModel.DataAnnotations.Schema;
using OpenNosCore.Database.Entities.Base;

namespace OpenNosCore.Database.Entities
{
    public class EquipmentOption : SynchronizableBaseEntity
    {
        #region Properties

        public byte Level { get; set; }

        public byte Type { get; set; }

        public int Value { get; set; }

        [ForeignKey(nameof(WearableInstanceId))]
        public virtual WearableInstance WearableInstance { get; set; }

        public Guid WearableInstanceId { get; set; }

        #endregion
    }
}