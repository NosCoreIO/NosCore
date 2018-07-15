using System.ComponentModel.DataAnnotations;
using NosCore.Shared.Enumerations.Buff;

namespace NosCore.Data.StaticEntities
{
	public class DropDTO : IDTO
	{
		public int Amount { get; set; }

		public int DropChance { get; set; }

		[Key]
        public short DropId { get; set; }

		public short ItemVNum { get; set; }

		public short? MapTypeId { get; set; }

		public short? MonsterVNum { get; set; }

    }
}