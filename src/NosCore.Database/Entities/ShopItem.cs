//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Database.Entities
{
    [StaticMetaData(LoadedMessage = LogLanguageKey.SHOPITEMS_LOADED)]
    public class ShopItem : IStaticEntity
    {
        public byte Color { get; set; }

        public virtual Item Item { get; set; } = null!;

        public short ItemVNum { get; set; }

        public short Rare { get; set; }

        public virtual Shop Shop { get; set; } = null!;

        public int ShopId { get; set; }

        [Key]
        public int ShopItemId { get; set; }

        public byte Slot { get; set; }

        public byte Type { get; set; }

        public byte Upgrade { get; set; }
    }
}
