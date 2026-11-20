//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Visibility;

namespace NosCore.GameObject.Ecs.Extensions;

public static class NpcBundleExtensions
{
    private const short LedGroupEffect = 3;

    public static InPacket GenerateIn(this NpcComponentBundle npc, short? dialog = null)
    {
        var packet = npc.GenerateIn(SpawnEffectType.NoEffect, dialog ?? npc.Dialog ?? 0);
        if (npc.LeaderId != 0 && packet.InNonPlayerSubPacket is { } nonPlayer)
        {
            nonPlayer.GroupEffect = LedGroupEffect;
            nonPlayer.Owner = npc.LeaderId;
        }

        return packet;
    }

    // Returns the HP actually given back, never past the maximum; a dead NPC gets none.
    public static int RestoreHp(this NpcComponentBundle npc, int hp)
    {
        var healed = npc.IsAlive ? Math.Max(Math.Min(npc.MaxHp - npc.Hp, hp), 0) : 0;
        npc.Hp += healed;
        return healed;
    }
}
