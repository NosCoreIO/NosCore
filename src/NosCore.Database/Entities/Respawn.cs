//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Respawn : IEntity
    {
        public virtual Character Character { get; set; } = null!;

        public long CharacterId { get; set; }

        public virtual Map Map { get; set; } = null!;

        public short MapId { get; set; }

        [Key]
        public long RespawnId { get; set; }

        public virtual RespawnMapType RespawnMapType { get; set; } = null!;

        public long RespawnMapTypeId { get; set; }

        public short X { get; set; }

        public short Y { get; set; }
    }
}
