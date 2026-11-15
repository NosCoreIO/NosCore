//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    public class ShopTypeService : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short ShopTypeServiceId { get; set; }

        public byte ShopType { get; set; }

        public NpcService Service { get; set; }
    }
}
