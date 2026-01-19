//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class RespawnMapType : IStaticEntity
    {
        public RespawnMapType()
        {
            Respawn = new HashSet<Respawn>();
            MapTypes = new HashSet<MapType>();
            MapTypes1 = new HashSet<MapType>();
        }

        public short MapId { get; set; }

        public short DefaultX { get; set; }

        public short DefaultY { get; set; }

        public virtual Map Map { get; set; } = null!;

        public virtual ICollection<MapType> MapTypes { get; set; }

        public virtual ICollection<MapType> MapTypes1 { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public virtual ICollection<Respawn> Respawn { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RespawnMapTypeId { get; set; }
    }
}
