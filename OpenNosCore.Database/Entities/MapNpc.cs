using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenNosCore.Database.Entities
{
    public class MapNpc
    {
        #region Instantiation

        public MapNpc()
        {
            Recipe = new HashSet<Recipe>();
            Shop = new HashSet<Shop>();
            Teleporter = new HashSet<Teleporter>();
        }

        #endregion

        #region Properties

        public short Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public virtual NpcMonster NpcMonster { get; set; }

        public short VNum { get; set; }

        public byte Direction { get; set; }

        public virtual ICollection<Recipe> Recipe { get; set; }

        public virtual ICollection<Shop> Shop { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }

        #endregion
    }
}