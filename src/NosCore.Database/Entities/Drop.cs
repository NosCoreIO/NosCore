//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Drop : IStaticEntity
    {
        public int Amount { get; set; }

        public int DropChance { get; set; }

        [Key]
        public short DropId { get; set; }

        public virtual Item Item { get; set; } = null!;

        public short VNum { get; set; }

        public virtual MapType? MapType { get; set; }

        public short? MapTypeId { get; set; }

        public short? MonsterVNum { get; set; }

        public virtual NpcMonster? NpcMonster { get; set; }
    }
}
