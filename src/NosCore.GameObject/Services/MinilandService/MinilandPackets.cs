//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs;
using NosCore.Packets.ServerPackets.Miniland;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MinilandService;

public static class MinilandPackets
{
    public static MlobjlstPacket GenerateMlobjlst(this PlayerComponentBundle player)
    {
        var mlobj = new List<MlobjlstSubPacket?>();
        foreach (var item in player.InventoryService.Where(s => s.Value.Type == NoscorePocketType.Miniland)
            .OrderBy(s => s.Value.Slot).Select(s => s.Value))
        {
            var used = player.MapInstance.MapDesignObjects.ContainsKey(item.Id);
            var mp = used ? player.MapInstance.MapDesignObjects[item.Id] : null;

            mlobj.Add(new MlobjlstSubPacket
            {
                InUse = used,
                Slot = item.Slot,
                MlObjSubPacket = new MlobjSubPacket
                {
                    MapX = mp?.MapX ?? 0,
                    MapY = mp?.MapY ?? 0,
                    Width = item.ItemInstance.Item.Width != 0 ? item.ItemInstance.Item.Width : (byte)1,
                    Height = item.ItemInstance.Item.Height != 0 ? item.ItemInstance.Item.Height : (byte)1,
                    DurabilityPoint = used ? item.ItemInstance.DurabilityPoint : 0,
                    Unknown = 100,
                    Unknown2 = false,
                    IsWarehouse = item.ItemInstance.Item.IsWarehouse
                }
            });
        }

        return new MlobjlstPacket
        {
            MlobjlstSubPacket = mlobj
        };
    }
}
