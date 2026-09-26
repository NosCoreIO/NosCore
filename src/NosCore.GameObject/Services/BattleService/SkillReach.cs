//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Distances are the truncated octile distance, the same integer the walk handler and the
// monster AI compare with.
public static class SkillReach
{
    private const byte TargetedSkill = 0;

    // The target may have stepped once between the click and the packet arriving.
    private const int LatencyCells = 1;

    // A range of 0 says nothing about reach in the skill data, so it is not gated.
    public static bool IsInReach(SkillInfo skill, int distance)
    {
        return skill.TargetType != TargetedSkill
            || skill.Range == 0
            || distance <= skill.Range + LatencyCells;
    }

    // Same allowance the walk handler gives one step at the entity's speed.
    public static bool IsWithinOneStep(int distance, byte speed)
    {
        return distance - 1 <= speed / 2;
    }
}
