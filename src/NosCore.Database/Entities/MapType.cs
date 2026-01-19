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
    public class MapType : IStaticEntity
    {
        public MapType()
        {
            MapTypeMap = new HashSet<MapTypeMap>();
            Drops = new HashSet<Drop>();
        }

        public virtual ICollection<Drop> Drops { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MapTypeId { get; set; }

        public virtual ICollection<MapTypeMap> MapTypeMap { get; set; }

        [Required]
        public required string MapTypeName { get; set; }

        public short PotionDelay { get; set; }

        public virtual RespawnMapType? RespawnMapType { get; set; }

        public long? RespawnMapTypeId { get; set; }

        public virtual RespawnMapType? ReturnMapType { get; set; }

        public long? ReturnMapTypeId { get; set; }
    }
}
