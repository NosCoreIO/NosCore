//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class RollGeneratedItem : IEntity
    {
        [Key]
        public short RollGeneratedItemId { get; set; }

        public short OriginalItemDesign { get; set; }

        public virtual Item OriginalItem { get; set; } = null!;

        public short OriginalItemVNum { get; set; }

        public short Probability { get; set; }

        public byte ItemGeneratedAmount { get; set; }

        public short ItemGeneratedVNum { get; set; }

        public byte ItemGeneratedUpgrade { get; set; }

        public bool IsRareRandom { get; set; }

        public short MinimumOriginalItemRare { get; set; }

        public short MaximumOriginalItemRare { get; set; }

        public virtual Item ItemGenerated { get; set; } = null!;

        public bool IsSuperReward { get; set; }
    }
}
