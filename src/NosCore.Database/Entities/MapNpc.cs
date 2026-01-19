//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.MAPNPCS_LOADED)]
    public class MapNpc : IEntity
    {
        public MapNpc()
        {
            Recipe = new HashSet<Recipe>();
            Shop = new HashSet<Shop>();
            Teleporter = new HashSet<Teleporter>();
        }

        public short? Dialog { get; set; }

        public short Effect { get; set; }

        public short EffectDelay { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public bool IsSitting { get; set; }

        public virtual Map Map { get; set; } = null!;

        public short MapId { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MapNpcId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public virtual NpcMonster NpcMonster { get; set; } = null!;

        public short VNum { get; set; }

        public byte Direction { get; set; }

        public virtual ICollection<Recipe> Recipe { get; set; }

        public virtual ICollection<Shop> Shop { get; set; }

        public virtual ICollection<Teleporter> Teleporter { get; set; }

        public virtual NpcTalk? NpcTalk { get; set; } = null!;
    }
}
