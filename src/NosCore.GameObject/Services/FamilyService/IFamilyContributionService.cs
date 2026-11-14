//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyContributionService
    {
        /// <summary>
        /// What a member earns for the family, whatever earned it: added on the master, written to the
        /// history under the member's name and shown to every online member.
        /// </summary>
        /// <returns>false when the character is not in that family or nothing was stored.</returns>
        Task<bool> ExperienceEarnedAsync(long familyId, long characterId, string characterName, int experience);

        Task CharacterLevelledUpAsync(long familyId, long characterId, string characterName, byte previousLevel, byte level);

        Task DailyMessageWrittenAsync(long familyId);

        Task InstantCombatWonAsync(long familyId, long characterId);
    }
}
