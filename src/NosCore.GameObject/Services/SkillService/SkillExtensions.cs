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
    }
}
