using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.AliveEntities
{
    public class MapNpcDTO : IDTO
    {  
        public short Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public short MapId { get; set; }

        [Key]
        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short VNum { get; set; }

        public byte Direction { get; set; }

        public void Initialize()
        {

        }
    }
}
