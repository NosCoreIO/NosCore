//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class Recipe : IStaticEntity
    {
        public Recipe()
        {
            RecipeItem = new HashSet<RecipeItem>();
        }

        public byte Amount { get; set; }

        public virtual Item Item { get; set; } = null!;

        public short ItemVNum { get; set; }

        public virtual MapNpc MapNpc { get; set; } = null!;

        public int MapNpcId { get; set; }

        [Key]
        public short RecipeId { get; set; }

        public virtual ICollection<RecipeItem> RecipeItem { get; set; }
    }
}
