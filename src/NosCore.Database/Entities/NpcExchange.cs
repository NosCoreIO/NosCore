//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using NosCore.Database.Entities.Base;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosCore.Database.Entities
{
    // An item an NPC dialog choice hands over for materials. A choice with several rows gives the
    // first one whose item fits the player's class.
    public class NpcExchange : IStaticEntity
    {
        public NpcExchange()
        {
            NpcExchangeMaterial = new HashSet<NpcExchangeMaterial>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short NpcExchangeId { get; set; }

        public NrunRunnerType Runner { get; set; }

        public virtual Item Item { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        // A buff the NPC casts on the player along with the item.
        public virtual Card? Card { get; set; }

        public short? CardId { get; set; }

        public virtual ICollection<NpcExchangeMaterial> NpcExchangeMaterial { get; set; }
    }
}
