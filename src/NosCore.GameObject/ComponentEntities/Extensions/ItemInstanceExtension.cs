using System.Collections.Generic;
using System.Linq;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class ItemInstanceExtension
    {
        public static IvnPacket GeneratePocketChange(this ItemInstance itemInstance, PocketType type, short slot)
        {
            if (itemInstance == null)
            {
                return new IvnPacket
                {
                    Type = type,
                    IvnSubPackets = new List<IvnSubPacket>{new IvnSubPacket()
                    {
                        Slot = slot,
                        VNum = -1,
                        RareAmount = 0,
                        UpgradeDesign = 0,
                        SecondUpgrade = 0
                    }}
                };
            }
            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = new List<IvnSubPacket>{new IvnSubPacket()
                {
                    Slot = slot,
                    VNum = itemInstance.ItemVNum,
                    RareAmount = itemInstance.Type != PocketType.Equipment ? itemInstance.Amount : itemInstance.Rare,
                    UpgradeDesign = itemInstance.Upgrade,
                    SecondUpgrade = 0
                }}
            };
        }

        public static IvnPacket GeneratePocketChange(this List<ItemInstance> itemInstance)
        {
            if (itemInstance.Count > 0)
            {
                return new IvnPacket
                {
                    Type = itemInstance.First().Type,
                    IvnSubPackets = itemInstance.Select(item => new IvnSubPacket()
                    {
                        Slot = item.Slot,
                        VNum = item.ItemVNum,
                        RareAmount = item.Type != PocketType.Equipment ? item.Amount : item.Rare,
                        UpgradeDesign = item.Upgrade,
                        SecondUpgrade = 0
                    }).ToList()
                };
            }

            return null;
        }
    }
}
