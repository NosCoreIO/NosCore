//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyProgressionService
    {
        /// <returns>null when the character is not in that family.</returns>
        Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience);

        /// <returns>null when the family does not exist.</returns>
        Task<FamilyExperienceGain?> AdvanceQuestsAsync(long familyId, FamilyQuestObjective objective, int amount);
    }
}
