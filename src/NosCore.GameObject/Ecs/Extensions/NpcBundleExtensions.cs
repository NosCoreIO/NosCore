//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Visibility;

namespace NosCore.GameObject.Ecs.Extensions;

public static class NpcBundleExtensions
{
    public static InPacket GenerateIn(this NpcComponentBundle npc, short? dialog = null) =>
        npc.GenerateIn(SpawnEffectType.NoEffect, dialog ?? npc.Dialog ?? 0);
}
