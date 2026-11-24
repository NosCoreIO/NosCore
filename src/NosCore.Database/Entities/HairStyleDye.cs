//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    // One hairstyle an item dyes; any other hairstyle refuses the item.
    public class HairStyleDye : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int HairStyleDyeId { get; set; }

        public short ItemVNum { get; set; }

        public virtual Item Item { get; set; }

        public HairStyleType HairStyle { get; set; }

        public HairStyleType DyedHairStyle { get; set; }
    }
}
