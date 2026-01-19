//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.MAPMONSTERS_LOADED)]
    public class MapMonster : IEntity
    {
        public bool IsDisabled { get; set; }

        public bool IsMoving { get; set; }

        public virtual Map Map { get; set; } = null!;

        public short MapId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int MapMonsterId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short VNum { get; set; }

        public virtual NpcMonster NpcMonster { get; set; } = null!;

        public byte Direction { get; set; }
    }
}
