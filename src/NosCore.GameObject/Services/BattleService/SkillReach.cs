//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.GameObject.Services.BattleService;

// Distances are the truncated octile distance, the same integer the walk handler and the
// monster AI compare with.
public static class SkillReach
{
    public const byte TargetedSkill = 0;
    public const byte SelfSkill = 1;
    public const byte AllySkill = 2;
    public const byte GroundSkill = 3;

    // The target may have stepped once between the click and the packet arriving.
    private const int LatencyCells = 1;

    // A skill without a range in its data is a melee one.
    private const int MeleeRange = 1;

    // A targeted or ally skill reaches its range; any other skill is centred on the caster, so the
    // target it names has to stand inside its area. A monster's body area widens the reach,
    // so a large monster can be hit from its edge.
    public static bool IsInReach(byte targetType, byte range, byte targetRange, int distance, byte targetArea = 0)
    {
        var reach = targetType is TargetedSkill or AllySkill ? Math.Max((int)range, MeleeRange) : targetRange;
        return distance <= reach + LatencyCells + targetArea;
    }

    // For casts that name their targets or their cell whatever the skill's target type says:
    // the cell of a ground skill, and each entity of a target list.
    public static bool IsTargetInReach(byte range, int distance, byte targetArea = 0)
    {
        return distance <= Math.Max((int)range, MeleeRange) + LatencyCells + targetArea;
    }

    // Same allowance the walk handler gives one step at the entity's speed.
    public static bool IsWithinOneStep(int distance, byte speed)
    {
        return distance - 1 <= speed / 2;
    }
}
