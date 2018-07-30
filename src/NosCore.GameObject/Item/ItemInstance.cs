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
        public Item Item => _item ?? (_item = ServerManager.Instance.Items.Find(item=>item.VNum == ItemVNum));

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