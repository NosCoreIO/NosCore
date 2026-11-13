//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class MonsterBundleExtensions
{
    public static void Respawn(this MonsterComponentBundle monster)
    {
        monster.Hp = monster.MaxHp;
        monster.Mp = monster.MaxMp;
        monster.IsAlive = true;
        monster.PositionX = monster.FirstX;
        monster.PositionY = monster.FirstY;
        monster.HitList.Clear();
    }

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
                SpawnEffect = SpawnEffectType.Summon,
                IsSitting = monster.IsSitting,
            }
        };
    }
}
