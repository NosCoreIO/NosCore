using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities.Base
{
	public abstract class SynchronizableBaseEntity
	{
		#region Properties

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public Guid Id { get; set; }

		#endregion
	}
}