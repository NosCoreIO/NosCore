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
        Task CharacterLevelledUpAsync(long familyId, long characterId, string characterName, byte previousLevel, byte level);

        Task DailyMessageWrittenAsync(long familyId);

        Task InstantCombatWonAsync(long familyId, long characterId);
    }
}
