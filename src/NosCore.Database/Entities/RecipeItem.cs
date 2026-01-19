//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    public class RecipeItem : IEntity
    {
        public short Amount { get; set; }

        public virtual Item Item { get; set; } = null!;

        public short ItemVNum { get; set; }

        public virtual Recipe Recipe { get; set; } = null!;

        public short RecipeId { get; set; }

        [Key]
        public short RecipeItemId { get; set; }
    }
}
