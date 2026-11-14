//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Visibility;

namespace NosCore.GameObject.Ecs.Extensions;

public static class MonsterBundleExtensions
{
    public static void Respawn(this MonsterComponentBundle monster)
    {
        monster.Hp = monster.MaxHp;
        monster.Mp = monster.MaxMp;
        monster.PositionX = monster.FirstX;
        monster.PositionY = monster.FirstY;
        monster.HitList.Clear();
        // Last: whoever sees it alive must already find it whole and at its spawn point.
        monster.IsAlive = true;
    }

    public static InPacket GenerateIn(this MonsterComponentBundle monster) =>
        monster.GenerateIn(SpawnEffectType.Summon);
}
