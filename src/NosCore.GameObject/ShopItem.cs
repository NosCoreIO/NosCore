using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.GameObject
{
    public class ShopItem
    {
        public IItemInstance ItemInstance { get; set; }

        public byte Type { get; set; }

        public short Slot { get; set; }

        public short? Price { get; set; }

        public short? Amount { get; set; }
    }
}
