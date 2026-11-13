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
    public class InstantCombatDrop : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short InstantCombatDropId { get; set; }

        /// <summary>The lowest level of the bracket that gets this drop.</summary>
        public byte LevelMinimum { get; set; }

        /// <summary>The wave whose end scatters this drop.</summary>
        public byte Wave { get; set; }

        public short ItemVNum { get; set; }

        /// <summary>How many piles are scattered over the map.</summary>
        public short Piles { get; set; }

        /// <summary>The amount in each pile.</summary>
        public short Amount { get; set; }
    }
}
