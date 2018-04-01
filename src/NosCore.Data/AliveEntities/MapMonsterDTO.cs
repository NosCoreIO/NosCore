using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.AliveEntities
{
    public class MapMonsterDTO : IDTO
    {
        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public short MapId { get; set; }

        [Key]
        public int MapMonsterId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short VNum { get; set; }

        public byte Direction { get; set; }
    }
}
