using System;
using System.ComponentModel.DataAnnotations.Schema;
using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
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