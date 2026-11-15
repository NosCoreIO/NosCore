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
    public class NpcExchangeMaterial : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short NpcExchangeMaterialId { get; set; }

        public virtual NpcExchange NpcExchange { get; set; }

        public short NpcExchangeId { get; set; }

        public virtual Item Item { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }
    }
}
