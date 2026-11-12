//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class NpcBundleExtensions
{
    public static InPacket GenerateIn(this NpcComponentBundle npc, short? dialog = null)
    {
        return new InPacket
        {
            VisualType = VisualType.Npc,
            VisualId = npc.VisualId,
            VNum = npc.VNum == 0 ? string.Empty : npc.VNum.ToString(),
            PositionX = npc.PositionX,
            PositionY = npc.PositionY,
            Direction = npc.Direction,
            InNonPlayerSubPacket = new InNonPlayerSubPacket
            {
                Dialog = dialog ?? npc.Dialog ?? 0,
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = npc.MaxHp > 0 ? (int)(npc.Hp / (float)npc.MaxHp * 100) : 100,
                    Mp = npc.MaxMp > 0 ? (int)(npc.Mp / (float)npc.MaxMp * 100) : 100
                },
                SpawnEffect = (SpawnEffectType)1,
                IsSitting = npc.IsSitting,
            }
        };
    }
}
