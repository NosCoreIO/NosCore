using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
	public class RespawnMapType
	{
		#region Instantiation

		public RespawnMapType()
		{
			Respawn = new HashSet<Respawn>();
			MapTypes = new HashSet<MapType>();
			MapTypes1 = new HashSet<MapType>();
		}

		#endregion

		#region Properties

		public short DefaultMapId { get; set; }

		public short DefaultX { get; set; }

		public short DefaultY { get; set; }

		public virtual Map Map { get; set; }

		public ICollection<MapType> MapTypes { get; set; }

		public ICollection<MapType> MapTypes1 { get; set; }

		[MaxLength(255)]
		public string Name { get; set; }

		public virtual ICollection<Respawn> Respawn { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long RespawnMapTypeId { get; set; }

		#endregion
	}
}