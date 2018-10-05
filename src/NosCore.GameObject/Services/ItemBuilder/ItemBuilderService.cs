using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapster;
using NosCore.Data;
using NosCore.GameObject.Item;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services
{ 
    public class ItemBuilderService : IItemBuilderService
    {
        private List<Item.Item> _items;

        public ItemBuilderService(List<Item.Item> items)
        {
            _items = items;
        }

        public ItemInstance Convert(ItemInstanceDTO k)
        {
            ItemInstance item = k.Adapt<ItemInstance>();
            item.Item = _items.Find(s => s.VNum == k.ItemVNum);
            return item;
        }

        public ItemInstance Create(short itemToCreateVNum, long characterId, short amount = 1, sbyte rare = 0, byte upgrade = 0, byte design = 0)
        {
            Item.Item itemToCreate = _items.Find(s => s.VNum == itemToCreateVNum);
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

    }
}
