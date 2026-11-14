//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class MountMapSpeed : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MountMapSpeedId { get; set; }

        public short ItemVNum { get; set; }

        public virtual Item Item { get; set; }

        public short MapId { get; set; }

        /// <summary>Added to the mount's riding speed on that map.</summary>
        public byte Speed { get; set; }
    }
}
