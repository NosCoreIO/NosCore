using NosCore.Data;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services.ItemBuilder.Item
{
    public class ItemInstance : ItemInstanceDTO
    {
        public bool IsBound => BoundCharacterId.HasValue && Item.ItemType != ItemType.Armor && Item.ItemType != ItemType.Weapon;

        public Item Item { get; set; }

        public ItemInstance(Item item)
        {
            Item = item;
            ItemVNum = item.VNum;
        }

        public ItemInstance()
        {
        }

        public ItemInstance Clone()
        {
            return (ItemInstance)MemberwiseClone();
        }
    }
}