//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class IItemInstanceExtension
    {
        public static IvnSubPacket GenerateIvnSubPacket(this IItemInstance? itemInstance, PocketType type,
            short slot)
        {
            if (itemInstance == null)
            {
                return new IvnSubPacket
                {
                    Slot = slot,
                    VNum = -1,
                    RareAmount = 0,
                    UpgradeDesign = 0,
                    SecondUpgrade = 0
                };
            }

            return new IvnSubPacket
            {
                Slot = slot,
                VNum = itemInstance.ItemVNum,
                RareAmount =
                    type != PocketType.Equipment ? itemInstance.Amount
                        : itemInstance.Rare,
                UpgradeDesign = itemInstance.Upgrade,
                SecondUpgrade = 0
            };
        }

        public static IvnPacket GeneratePocketChange(this InventoryItemInstance? itemInstance,
            PocketType type,
            short slot)
        {
            if (itemInstance == null)
            {
                return new IvnPacket
                {
                    Type = type,
                    IvnSubPackets = new List<IvnSubPacket?> { ((IItemInstance?)null).GenerateIvnSubPacket(type, slot) }
                };
            }

            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = new List<IvnSubPacket?>
                {
                    new()
                    {
                        Slot = slot,
                        VNum = itemInstance.ItemInstance.ItemVNum,
                        RareAmount =
                            itemInstance.Type != NoscorePocketType.Equipment ? itemInstance.ItemInstance.Amount
                                : itemInstance.ItemInstance.Rare,
                        UpgradeDesign = itemInstance.ItemInstance.Upgrade,
                        SecondUpgrade = 0
                    }
                }
            };
        }

        public static IvnPacket? GeneratePocketChange(this List<InventoryItemInstance> itemInstance)
        {
            if (itemInstance.Count <= 0)
            {
                return null;
            }

            var type = (PocketType)itemInstance[0].Type;
            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = itemInstance.Select(item => item.ItemInstance.GenerateIvnSubPacket(type, item.Slot))
                    .ToList() as List<IvnSubPacket?>
            };

        }
    }
}
