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
    public class MinigameReward : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short MinigameRewardId { get; set; }

        public short MinigameVNum { get; set; }

        public virtual Item Minigame { get; set; }

        /// <summary>The reward box it comes from, 0 for the lowest score band.</summary>
        public byte Level { get; set; }

        public short ItemVNum { get; set; }

        public virtual Item Item { get; set; }

        public short Amount { get; set; }
    }
}
