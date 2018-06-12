using NosCore.Database.Entities.Base;

namespace NosCore.Database.Entities
{
	public class QuicklistEntry : SynchronizableBaseEntity
	{
		#region Properties

		public virtual Character Character { get; set; }

		public long CharacterId { get; set; }

		public short Morph { get; set; }

		public short Pos { get; set; }

		public short Q1 { get; set; }

		public short Q2 { get; set; }

		public short Slot { get; set; }

		public short Type { get; set; }

		#endregion
	}
}