//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.RECIPES_LOADED)]
    public class Recipe : IStaticEntity
    {
        public Recipe()
        {
            RecipeItem = new HashSet<RecipeItem>();
        }

        public byte Amount { get; set; }

        public virtual Item Item { get; set; }

        public short ItemVNum { get; set; }

        public virtual MapNpc? MapNpc { get; set; }

        public int? MapNpcId { get; set; }

        public virtual Item? Scroll { get; set; }

        public short? ScrollVNum { get; set; }

        [Key]
        public short RecipeId { get; set; }

        public virtual ICollection<RecipeItem> RecipeItem { get; set; }
    }
}
