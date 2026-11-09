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
using NosCore.Packets.ServerPackets.Warehouse;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Extensions
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
                    Slot = slot
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
                SecondUpgrade = itemInstance is SpecialistInstance specialist ? specialist.SpStoneUpgrade : (byte)0
            };
        }

        public static WarehouseSubPacket GenerateWarehouseSubPacket(this IItemInstance? itemInstance, short slot)
        {
            if (itemInstance == null)
            {
                return new WarehouseSubPacket { Slot = slot };
            }

            var type = itemInstance is SpecialistInstance ? PocketType.Specialist : itemInstance.Item.Type.ToPocketType();
            return type switch
            {
                PocketType.Equipment => new WarehouseSubPacket
                {
                    Slot = slot, VNum = itemInstance.ItemVNum, Type = type, Amount = itemInstance.Amount,
                    Rare = itemInstance.Rare, Upgrade = itemInstance.Upgrade
                },
                PocketType.Specialist => new WarehouseSubPacket
                {
                    Slot = slot, VNum = itemInstance.ItemVNum, Type = type, Amount = itemInstance.Upgrade,
                    Rare = itemInstance is SpecialistInstance specialist ? specialist.SpStoneUpgrade : (short)0
                },
                _ => new WarehouseSubPacket
                {
                    Slot = slot, VNum = itemInstance.ItemVNum, Type = type, Amount = itemInstance.Amount
                }
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
                    IvnSubPackets = new List<IvnSubPacket> { ((IItemInstance?)null).GenerateIvnSubPacket(type, slot) }
                };
            }

            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = new List<IvnSubPacket>
                {
                    itemInstance.ItemInstance.GenerateIvnSubPacket(itemInstance.Type.ToPocketType(), slot)
                }
            };
        }

        public static IvnPacket? GeneratePocketChange(this List<InventoryItemInstance> itemInstance)
        {
            if (itemInstance.Count <= 0)
            {
                return null;
            }

            var type = itemInstance[0].Type.ToPocketType();
            return new IvnPacket
            {
                Type = type,
                IvnSubPackets = itemInstance.Select(item => item.ItemInstance.GenerateIvnSubPacket(type, item.Slot))
                    .ToList()
            };

        }
    }
}
