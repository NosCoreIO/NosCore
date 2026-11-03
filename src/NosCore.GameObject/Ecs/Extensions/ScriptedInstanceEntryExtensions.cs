//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions;

public static class ScriptedInstanceEntryExtensions
{
    public static InstanceGift? MissingRequiredItem(this PlayerComponentBundle player, ScriptedInstance entrance)
    {
        var required = entrance.Definition?.RequiredItems ?? [];
        return required.FirstOrDefault(item => player.InventoryService.Values
            .Where(s => s.ItemInstance.ItemVNum == item.VNum)
            .Sum(s => s.ItemInstance.Amount) < item.Amount);
    }

    public static async Task TakeRequiredItemsAsync(this PlayerComponentBundle player, ScriptedInstance entrance)
    {
        foreach (var item in entrance.Definition?.RequiredItems ?? [])
        {
            var remaining = item.Amount;
            var stacks = player.InventoryService.Values
                .Where(s => s.ItemInstance.ItemVNum == item.VNum)
                .OrderBy(s => s.Type).ThenBy(s => s.Slot)
                .ToList();
            foreach (var stack in stacks)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var taken = (short)Math.Min(remaining, stack.ItemInstance.Amount);
                player.InventoryService.RemoveItemAmountFromInventory(taken, stack.ItemInstanceId);
                remaining -= taken;
                await player.SendPacketAsync(stack.GeneratePocketChange((PocketType)stack.Type, stack.Slot));
            }
        }
    }
}
