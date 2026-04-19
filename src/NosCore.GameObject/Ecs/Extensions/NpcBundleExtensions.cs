//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Movement;
using NosCore.Packets.ServerPackets.Player;
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
            VNum = npc.VNum.ToString(),
            PositionX = npc.PositionX,
            PositionY = npc.PositionY,
            Direction = npc.Direction,
            InNonPlayerSubPacket = new InNonPlayerSubPacket
            {
                Dialog = dialog ?? 0,
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = npc.MaxHp > 0 ? (int)(npc.Hp / (float)npc.MaxHp * 100) : 100,
                    Mp = npc.MaxMp > 0 ? (int)(npc.Mp / (float)npc.MaxMp * 100) : 100
                },
                IsSitting = npc.IsSitting,
                SpawnEffect = SpawnEffectType.NoEffect,
                Unknow1 = 2
            }
        };
    }

    public static CondPacket GenerateCond(this NpcComponentBundle npc)
    {
        return new CondPacket
        {
            VisualType = VisualType.Npc,
            VisualId = npc.VisualId,
            NoAttack = npc.NoAttack,
            NoMove = npc.NoMove,
            Speed = npc.Speed
        };
    }

    public static MovePacket GenerateMove(this NpcComponentBundle npc, short? mapX = null, short? mapY = null)
    {
        return new MovePacket
        {
            VisualType = VisualType.Npc,
            VisualEntityId = npc.VisualId,
            MapX = mapX ?? npc.PositionX,
            MapY = mapY ?? npc.PositionY,
            Speed = npc.Speed
        };
    }

    public static CharScPacket GenerateCharSc(this NpcComponentBundle npc)
    {
        return new CharScPacket
        {
            VisualType = VisualType.Npc,
            VisualId = npc.VisualId,
            Size = npc.Size
        };
    }
}
