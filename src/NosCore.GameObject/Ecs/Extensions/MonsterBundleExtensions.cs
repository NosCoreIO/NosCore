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

public static class MonsterBundleExtensions
{
    public static InPacket GenerateIn(this MonsterComponentBundle monster)
    {
        return new InPacket
        {
            VisualType = VisualType.Monster,
            VisualId = monster.VisualId,
            VNum = monster.VNum == 0 ? string.Empty : monster.VNum.ToString(),
            PositionX = monster.PositionX,
            PositionY = monster.PositionY,
            Direction = monster.Direction,
            InNonPlayerSubPacket = new InNonPlayerSubPacket
            {
                Dialog = 0,
                InAliveSubPacket = new InAliveSubPacket
                {
                    Hp = monster.MaxHp > 0 ? (int)(monster.Hp / (float)monster.MaxHp * 100) : 100,
                    Mp = monster.MaxMp > 0 ? (int)(monster.Mp / (float)monster.MaxMp * 100) : 100
                },
                SpawnEffect = (SpawnEffectType)1,
                IsSitting = monster.IsSitting,
            }
        };
    }

    public static CondPacket GenerateCond(this MonsterComponentBundle monster)
    {
        return new CondPacket
        {
            VisualType = VisualType.Monster,
            VisualId = monster.VisualId,
            NoAttack = monster.NoAttack,
            NoMove = monster.NoMove,
            Speed = monster.Speed
        };
    }

    public static MovePacket GenerateMove(this MonsterComponentBundle monster, short? mapX = null, short? mapY = null)
    {
        return new MovePacket
        {
            VisualType = VisualType.Monster,
            VisualEntityId = monster.VisualId,
            MapX = mapX ?? monster.PositionX,
            MapY = mapY ?? monster.PositionY,
            Speed = monster.Speed
        };
    }

    public static CharScPacket GenerateCharSc(this MonsterComponentBundle monster)
    {
        return new CharScPacket
        {
            VisualType = VisualType.Monster,
            VisualId = monster.VisualId,
            Size = monster.Size
        };
    }
}
