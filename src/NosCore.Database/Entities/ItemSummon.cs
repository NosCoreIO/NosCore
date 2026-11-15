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
    // One monster an item can release; using the item picks one of its rows at random.
    public class ItemSummon : IStaticEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ItemSummonId { get; set; }

        public short ItemVNum { get; set; }

        public virtual Item Item { get; set; }

        public short NpcMonsterVNum { get; set; }

        public virtual NpcMonster NpcMonster { get; set; }
    }
}
