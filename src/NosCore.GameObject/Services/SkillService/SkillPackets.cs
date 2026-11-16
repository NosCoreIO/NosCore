//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.Packets.ServerPackets.Player;
using System.Linq;

namespace NosCore.GameObject.Services.SkillService;

public static class SkillPackets
{
    public static SkiPacket GenerateSki(this PlayerComponentBundle characterEntity, bool useSpecialist)
    {
        // The client takes the first listed skill for the basic attack, so passives go last.
        var ordered = characterEntity.Skills.Values
            .Where(s => s.Skill != null && s.Skill.IsSpecialist() == useSpecialist)
            .OrderBy(s => s.Skill.IsPassive())
            .ThenBy(s => s.Skill.CastId)
            .Select(s => s.SkillVNum)
            .ToList();

        var classByte = (byte)characterEntity.Class;
        var cardAttack = ordered.Count > 0 ? ordered[0] : (short)0;
        return new SkiPacket
        {
            PrimarySkillVnum = useSpecialist ? cardAttack : (short)(200 + 20 * classByte),
            SecondarySkillVnum = useSpecialist ? cardAttack : (short)(201 + 20 * classByte),
            SkillVnums = ordered,
        };
    }
}
