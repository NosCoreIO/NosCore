//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ShopService;

public static class ShopPackets
{
    public static NInvPacket GenerateSkillNInv(this IAliveEntity aliveEntity, Shop shop, short tab,
        CharacterClassType viewerClass)
    {
        return new NInvPacket
        {
            VisualType = aliveEntity.VisualType,
            VisualId = aliveEntity.VisualId,
            Unknown = 0,
            ShopKind = (byte)tab,
            Skills = shop.ShopSkills
                .Where(s => s.Type == tab && (s.Skill.IsPassive() || s.Skill.Class == (byte)viewerClass))
                .OrderBy(s => s.Slot).Select(s => s.SkillVNum).ToList()
        };
    }

    public static NInvPacket GenerateNInv(this IAliveEntity aliveEntity, Shop shop, double percent, short typeshop)
    {
        var shopItemList = new List<NInvItemSubPacket?>();
        var list = shop.ShopItems.Values.Where(s => s.Type == typeshop).ToList();
        for (var i = 0; i < shop.Size; i++)
        {
            var item = list.Find(s => s.Slot == i);
            if (item == null)
            {
                shopItemList.Add(null);
            }
            else
            {
                shopItemList.Add(new NInvItemSubPacket
                {
                    Type = item.ItemInstance.Item.Type.ToPocketType(),
                    Slot = item.Slot,
                    Price = (int)(item.Price ?? (item.ItemInstance.Item.ReputPrice > 0
                        ? item.ItemInstance.Item.ReputPrice : item.ItemInstance.Item.Price * percent)),
                    RareAmount = item.ItemInstance.Item.Type == NoscorePocketType.Equipment
                        ? item.ItemInstance.Rare
                        : item.Amount,
                    UpgradeDesign = item.ItemInstance.Item.Type == NoscorePocketType.Equipment
                        ? item.ItemInstance.Item.IsColored
                            ? item.ItemInstance.Item.Color : item.ItemInstance.Upgrade : (short?)null,
                    VNum = item.ItemInstance.Item.VNum
                });
            }
        }

        return new NInvPacket
        {
            VisualType = aliveEntity.VisualType,
            VisualId = aliveEntity.VisualId,
            ShopKind = (byte)(percent * 100),
            Unknown = 0,
            Items = shopItemList
        };
    }
}
