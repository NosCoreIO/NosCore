//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyContributionService(IFamilyLogService familyLogService, IFamilyHub familyHub,
        IPubSubHub pubSubHub) : IFamilyContributionService
    {
        // A history trace holds experience lines of 1 000, 5 000, 10 000, 50 000 and 100 000 only, and
        // none of 600, 800 or 1 200 beside its level 30, 40 and 60 lines, so this amount is suspect.
        private const int ExperiencePerLevel = 20;

        private static bool IsRewardedLevel(int level) => level > 20 && level % 10 == 0;

        public async Task CharacterLevelledUpAsync(long familyId, long characterId, string characterName,
            byte previousLevel, byte level)
        {
            await familyLogService.AddLevelUpsAsync(familyId, characterName, previousLevel, level);

            for (var reached = previousLevel + 1; reached <= level; reached++)
            {
                if (IsRewardedLevel(reached)
                    && !await ExperienceEarnedAsync(familyId, characterId, characterName, ExperiencePerLevel * reached))
                {
                    return;
                }
            }
        }

        public async Task<bool> ExperienceEarnedAsync(long familyId, long characterId, string characterName, int experience)
        {
            var gain = await familyHub.AddExperienceAsync(familyId, characterId, experience);
            if (gain == null)
            {
                return false;
            }

            await familyLogService.AddAsync(familyId, FamilyLogType.FamilyXp, characterName, experience);
            await AnnounceAsync(familyId, gain);
            return true;
        }

        public Task DailyMessageWrittenAsync(long familyId) => AdvanceAsync(familyId, null, FamilyQuestObjective.DailyMessage);

        public Task InstantCombatWonAsync(long familyId, long characterId) =>
            AdvanceAsync(familyId, characterId, FamilyQuestObjective.InstantCombat);

        private async Task AdvanceAsync(long familyId, long? characterId, FamilyQuestObjective objective)
        {
            var gain = await familyHub.AdvanceQuestsAsync(familyId, characterId, objective, 1);
            await AnnounceAsync(familyId, gain ?? new FamilyExperienceGain(0, false));
        }

        private async Task AnnounceAsync(long familyId, FamilyExperienceGain gain)
        {
            if (gain.LeveledUp)
            {
                await familyLogService.AddAsync(familyId, FamilyLogType.FamilyLevelUp, gain.FamilyLevel);
            }

            await pubSubHub.SendMessageAsync(new FamilyChangedData
            {
                FamilyId = familyId,
                ReachedFamilyLevel = gain.LeveledUp ? gain.FamilyLevel : null,
                CompletedQuestIds = [.. gain.CompletedQuestIds]
            });
        }
    }
}
