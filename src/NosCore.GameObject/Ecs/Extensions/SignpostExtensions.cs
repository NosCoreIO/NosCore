//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class SignpostExtensions
{
    public static async Task TakeDownAsync(this NpcComponentBundle signpost)
    {
        var map = signpost.MapInstance;
        var visualId = signpost.VisualId;
        if (!map.RemoveNpc(signpost))
        {
            return;
        }

        await map.SendPacketsAsync([
            new DiePacket { VisualType = VisualType.Npc, VisualId = visualId, TargetVisualType = VisualType.Npc, TargetId = visualId },
            new OutPacket { VisualType = VisualType.Npc, VisualId = visualId },
        ]);
    }
}
