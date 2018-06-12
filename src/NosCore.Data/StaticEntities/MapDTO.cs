using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
	public class MapDTO : IDTO
	{
		[Key]
		public short MapId { get; set; }

		[MaxLength(255)]
		public string Name { get; set; }

		public byte[] Data { get; set; }

		public int Music { get; set; }

		public bool ShopAllowed { get; set; }
	}
}