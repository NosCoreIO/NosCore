using NosCore.Data;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Items;
using System.Collections.Generic;

namespace NosCore.GameObject.Item
{
    public class ItemInstance : ItemInstanceDTO
    {
        public ItemInstance(List<Item> items)
        {
            _items = items;
        }
        public bool IsBound => BoundCharacterId.HasValue && Item.ItemType != ItemType.Armor && Item.ItemType != ItemType.Weapon;
        private Item _item;
        private List<Item> _items;

        public Item Item => _item ?? (_item = _items.Find(item => item.VNum == ItemVNum));
        public ItemInstance(Item item)
        {
            ItemVNum = item.VNum;
        }
        
        public static ItemInstance Create(Item itemToCreate, long characterId, short amount = 1, sbyte rare = 0, byte upgrade = 0, byte design = 0)
        {
            switch (itemToCreate.Type)
            {
                case PocketType.Miniland:
                    return new ItemInstance(itemToCreate)
                    {
                        CharacterId = characterId,
                        Amount = amount,
                        DurabilityPoint = itemToCreate.MinilandObjectPoint / 2
                    };

                case PocketType.Equipment:
                    switch (itemToCreate.ItemType)
                    {
                        case ItemType.Specialist:
                            return new SpecialistInstance(itemToCreate)
                            {
                                SpLevel = 1,
                                Amount = amount,
                                CharacterId = characterId,
                                Design = design,
                                Upgrade = upgrade
                            };
                        case ItemType.Box:
                            return new BoxInstance(itemToCreate)
                            {
                                Amount = amount,
                                CharacterId = characterId,
                                Upgrade = upgrade,
                                Design = design
                            };
                        default:
                            var wear = new WearableInstance(itemToCreate)
                            {
                                Amount = amount,
                                Rare = rare,
                                CharacterId = characterId,
                                Upgrade = upgrade,
                                Design = design
                            };
                            if (wear.Rare > 0)
                            {
                                wear.SetRarityPoint();
                            }

                            return wear;
                    }

                default:
                    return new ItemInstance(itemToCreate)
                    {
                        Type = itemToCreate.Type,
                        Amount = amount,
                        CharacterId = characterId
                    };
            }
        }

        public ItemInstance Clone()
        {
            return (ItemInstance)MemberwiseClone();
        }
    }
}