using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.StaticEntities
{
    public class DropDTO : IDTO
    {
        public int Amount { get; set; }

        public int DropChance { get; set; }

        [Key]
        public short DropId { get; set; }

        public short VNum { get; set; }

        public short? MapTypeId { get; set; }

        public short? MonsterVNum { get; set; }
    }
}