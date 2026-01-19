//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.ShopService
{
    public class ShopItem : ShopItemDto
    {
        public IItemInstance? ItemInstance { get; set; }

        public long? Price { get; set; }

        public short? Amount { get; set; }
    }
}
