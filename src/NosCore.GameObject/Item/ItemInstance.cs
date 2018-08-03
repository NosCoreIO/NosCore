using System;
using System.Linq;
using NosCore.Data;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Item
{
    public class ItemInstance : ItemInstanceDTO
    {
        public bool IsBound => BoundCharacterId.HasValue && Item.ItemType != ItemType.Armor && Item.ItemType != ItemType.Weapon;
        private Item _item;
        public Item Item => _item ?? (_item = ServerManager.Instance.Items.Find(item => item.VNum == ItemVNum));
        public ItemInstance(Item item)
        {
            ItemVNum = item.VNum;
        }

        public ItemInstance()
        {
        }

        public static ItemInstance Create(short itemVNum, long characterId, short amount = 1, sbyte rare = 0, byte upgrade = 0, byte design = 0)
        {
            var itemToCreate = ServerManager.Instance.Items.Find(item => item.VNum == itemVNum);
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

        public string GenerateFStash()
        {
            return $"f_stash {GenerateStashPacket()}";
        }

        public string GenerateInventoryAdd()
        {
            switch (Type)
            {
                case PocketType.Equipment:
                    return $"ivn 0 {Slot}.{ItemVNum}.{Rare}.{(Item.IsColored ? Design : Upgrade)}.0";

                case PocketType.Main:
                    return $"ivn 1 {Slot}.{ItemVNum}.{Amount}.0";

                case PocketType.Etc:
                    return $"ivn 2 {Slot}.{ItemVNum}.{Amount}.0";

                case PocketType.Miniland:
                    return $"ivn 3 {Slot}.{ItemVNum}.{Amount}";

                case PocketType.Specialist:
                    return $"ivn 6 {Slot}.{ItemVNum}.{Rare}.{Upgrade}.{(this as SpecialistInstance)?.SpStoneUpgrade}";

                case PocketType.Costume:
                    return $"ivn 7 {Slot}.{ItemVNum}.{Rare}.{Upgrade}.0";
            }
            return string.Empty;
        }

        public string GeneratePStash()
        {
            return $"pstash {GenerateStashPacket()}";
        }

        public string GenerateStash()
        {
            return $"stash {GenerateStashPacket()}";
        }

        public string GenerateStashPacket()
        {
            var packet = $"{Slot}.{ItemVNum}.{(byte)Item.Type}";
            switch (Item.Type)
            {
                case PocketType.Equipment:
                    return packet + $".{Amount}.{Rare}.{Upgrade}";

                case PocketType.Specialist:
                    SpecialistInstance sp = this as SpecialistInstance;
                    return packet + $".{Upgrade}.{sp?.SpStoneUpgrade ?? 0}.0";

                default:
                    return packet + $".{Amount}.0.0";
            }
        }

        public ItemInstance Clone()
        {
            return (ItemInstance)MemberwiseClone();
        }
    }
}