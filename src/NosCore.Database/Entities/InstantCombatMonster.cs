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
    public class InstantCombatMonster : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short InstantCombatMonsterId { get; set; }

        /// <summary>The lowest level of the bracket that fights this wave.</summary>
        public byte LevelMinimum { get; set; }

        public byte Wave { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short Amount { get; set; }

        public bool IsMoving { get; set; }
    }
}
