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
    [StaticMetaData(LoadedMessage = LogLanguageKey.SHOPS_LOADED)]
    public class Shop : IStaticEntity
    {
        public Shop()
        {
            ShopItem = new HashSet<ShopItem>();
            ShopSkill = new HashSet<ShopSkill>();
        }

        public virtual MapNpc MapNpc { get; set; } = null!;

        public int MapNpcId { get; set; }

        public byte MenuType { get; set; }

        [Key]
        public int ShopId { get; set; }

        public virtual ICollection<ShopItem> ShopItem { get; set; }

        public virtual ICollection<ShopSkill> ShopSkill { get; set; }

        public byte ShopType { get; set; }
    }
}
