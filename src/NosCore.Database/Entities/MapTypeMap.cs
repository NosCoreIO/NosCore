//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class MapTypeMap : IStaticEntity
    {
        [Key]
        public short MapTypeMapId { get; set; }

        public virtual Map Map { get; set; }

        public short MapId { get; set; }

        public virtual MapType MapType { get; set; }

        public short MapTypeId { get; set; }
    }
}
