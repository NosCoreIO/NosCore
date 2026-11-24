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
    // The gold and materials an upgrade window charges whatever is placed in it.
    public class UpgradeCost : IStaticEntity
    {
        public UpgradeCost()
        {
            UpgradeCostMaterial = new HashSet<UpgradeCostMaterial>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short UpgradeCostId { get; set; }

        public UpgradePacketType Kind { get; set; }

        public long Gold { get; set; }

        public virtual ICollection<UpgradeCostMaterial> UpgradeCostMaterial { get; set; }
    }
}
