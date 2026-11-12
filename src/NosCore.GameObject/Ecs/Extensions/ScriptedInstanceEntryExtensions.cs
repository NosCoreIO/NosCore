//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Ecs.Extensions;

public static class ScriptedInstanceEntryExtensions
{
    public static InstanceGift? MissingRequiredItem(this PlayerComponentBundle player, ScriptedInstance entrance) =>
        (entrance.Definition?.RequiredItems ?? [])
            .FirstOrDefault(item => player.InventoryService.CountItem(item.VNum) < item.Amount);

    public static async Task TakeRequiredItemsAsync(this PlayerComponentBundle player, ScriptedInstance entrance)
    {
        foreach (var item in entrance.Definition?.RequiredItems ?? [])
        {
            await player.SendPacketsAsync(player.InventoryService.RemoveItemAmountByVNum(item.VNum, item.Amount));
        }
    }
}
