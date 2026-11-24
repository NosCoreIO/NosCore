//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.SkillService
{
    public static class SkillExtensions
    {
        // Training courses, PvP boosts, tomes: never cast, and not tied to a class. Their CastId
        // is the passive's kind, so it overlaps the cast ids of real skills.
        public static bool IsPassive(this SkillDto skill) => skill.SkillType == 0;

        // Strengthens the skill named in UpgradeSkill, which has to be known first.
        public static bool IsUpgrade(this SkillDto skill) => skill.SkillType == 2;

        // A partner card's skills and a pet's own: the only kind a NosMate's skill bar holds.
        public static bool IsMateSkill(this SkillDto skill) => skill.SkillType == 5;

        // Every tattoo is a class 27 skill whose cast id is 39 plus its loa, the number a tattoo
        // pattern carries in EffectValue (bear 1, lion 2, eagle 3, snake 4, corrupt shadow 5).
        public static bool IsTattoo(this SkillDto skill) => skill.Class == 27;

        public static bool IsTattooOfLoa(this SkillDto skill, int loa) => skill.IsTattoo() && skill.CastId == 39 + loa;

        // Specialist card skills start at the pyjama card's class.
        public static bool IsSpecialist(this SkillDto skill) => skill.Class >= 32;
    }
}
