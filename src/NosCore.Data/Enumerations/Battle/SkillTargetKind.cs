//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Battle
{
    // The last TARGET column of Skill.dat: what a skill does to the entities it reaches.
    public enum SkillTargetKind : byte
    {
        Attack = 0,
        DebuffWithoutDamage = 1,
        Support = 2,
        CoupleEmote = 3
    }
}
