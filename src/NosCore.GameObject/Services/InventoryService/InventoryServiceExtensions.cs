//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.ServerPackets.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.InventoryService
{
    public static class InventoryServiceExtensions
    {
        // Takes the amount from as many stacks as it needs, never from what is worn, and returns
        // the refresh for every stack it touched.
        public static List<IvnPacket> RemoveItemAmountByVNum(this IInventoryService inventory, int vNum, int amount)
        {
            var packets = new List<IvnPacket>();
            var stacks = inventory.Values
                .Where(s => s.Type != NoscorePocketType.Wear && s.ItemInstance?.ItemVNum == vNum)
                .OrderBy(s => s.Type).ThenBy(s => s.Slot)
                .ToList();
            foreach (var stack in stacks)
            {
                if (amount <= 0)
                {
                    break;
                }

                var taken = (short)Math.Min(amount, stack.ItemInstance.Amount);
                var left = inventory.RemoveItemAmountFromInventory(taken, stack.ItemInstanceId);
                amount -= taken;
                packets.Add(left.GeneratePocketChange(stack.Type.ToPocketType(), stack.Slot));
            }

            return packets;
        }
    }
}
